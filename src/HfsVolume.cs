using System.Buffers.Binary;
using System.Diagnostics;

namespace HfsReader;

/// <summary>
/// Represents a Hierarchical File System (Hfs) volume and provides access to its contents.
/// </summary>
public class HfsVolume
{
    private readonly Stream _stream;
    private readonly int _streamStartOffset;

    /// <summary>
    /// Gets the boot block header of the HFS volume.
    /// </summary>
    public HfsBootBlockHeader BootBlock { get; }

    /// <summary>
    /// Gets the master directory block of the HFS volume.
    /// </summary>
    public HfsMasterDirectoryBlock MasterDirectoryBlock { get; }

    /// <summary>
    /// Gets the catalog B-tree of the HFS volume.
    /// </summary>
    public BTree<HfsCatalogKey, HfsCatalogKeyComparison> CatalogTree { get; }

    /// <summary>
    /// Gets the extents overflow B-tree of the HFS volume.
    /// </summary>
    public BTree<HfsExtentsKey, HfsExtentsKeyComparison>? ExtentsOverflowTree { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HfsVolume"/> class.
    /// </summary>
    /// <param name="stream">The stream containing the HFS volume data.</param>
    /// <param name="volumeStartOffset">The start offset of the volume within the stream.</param>
    public HfsVolume(Stream stream, int volumeStartOffset)
    {
        _stream = stream;
        _streamStartOffset = volumeStartOffset;

        // The first two blocks are the boot block - they can be skipped
        // for our purposes.
        stream.Seek(_streamStartOffset + 1024, SeekOrigin.Begin);

        // The next block is the master directory block.
        Span<byte> blockBuffer = stackalloc byte[512];
        if (stream.Read(blockBuffer) != blockBuffer.Length)
        {
            throw new InvalidDataException("Unable to read DSK master directory block.");
        }

        MasterDirectoryBlock = new HfsMasterDirectoryBlock(blockBuffer);

        // Initialize the catalog B-tree
        CatalogTree = new(_stream, _streamStartOffset, MasterDirectoryBlock.CatalogFileExtents, MasterDirectoryBlock.ExtentsStartBlockNumber, MasterDirectoryBlock.AllocationBlockSize);

        // Initialize the extents overflow B-tree if it exists
        if (MasterDirectoryBlock.ExtentsOverflowFileSize > 0)
        {
            ExtentsOverflowTree = new(_stream, _streamStartOffset, MasterDirectoryBlock.ExtentsOverflowExtents, MasterDirectoryBlock.ExtentsStartBlockNumber, MasterDirectoryBlock.AllocationBlockSize);
        }
    }

    /// <summary>
    /// Gets the contents of the root directory of the HFS volume.
    /// </summary>
    /// <returns>An enumerable of <see cref="HfsNode"/> objects in the root directory.</returns>
    public IEnumerable<HfsNode> RootContents() => ContentsOfDirectory((uint)HfsKnownCatalogNodeID.kHfsRootParentID);

    /// <summary>
    /// Gets the contents of the specified directory.
    /// </summary>
    /// <param name="directory">The directory whose contents to retrieve.</param>
    /// <returns>An enumerable of <see cref="HfsNode"/> objects in the directory.</returns>
    public IEnumerable<HfsNode> ContentsOfDirectory(HfsDirectory directory)
    {
        ArgumentNullException.ThrowIfNull(directory);
        return [.. ContentsOfDirectory(directory.Identifier)];
    }

    private IEnumerable<HfsNode> ContentsOfDirectory(uint parentIdentifier)
    {
        var comparison = new HfsCatalogKeyComparison(parentIdentifier, string.Empty);
        var currentNode = CatalogTree.FindFirstMatchingLeafNode(comparison);
        while (currentNode != null)
        {
            for (int i = 0; i < currentNode.Value.Descriptor.RecordCount; i++)
            {
                var recordOffset = currentNode.Value.RecordOffsets[i];
                var key = new HfsCatalogKey(CatalogTree.BlockBuffer.Slice(recordOffset.Offset, recordOffset.Size), out var bytesRead);

                // Data records are placed immediately after the key length byte and key data,
                // then padded to an even boundary. The key length does NOT include the length byte.
                var dataOffset = recordOffset.Offset + bytesRead;
                if ((dataOffset % 2) != 0)
                {
                    dataOffset += 1; // word-align
                }

                if (key.ParentIdentifier > parentIdentifier)
                {
                    yield break;
                }
                if (key.ParentIdentifier == parentIdentifier)
                {
                    var type = (HfsCatalogDataRecordType)BinaryPrimitives.ReadUInt16BigEndian(CatalogTree.BlockBuffer[dataOffset..]);
                    switch (type)
                    {
                        case HfsCatalogDataRecordType.File:
                            var fileRecord = new HfsFileRecord(CatalogTree.BlockBuffer.Slice(dataOffset, HfsFileRecord.Size));
                            yield return new HfsFile(
                                key.ParentIdentifier,
                                key.Name,
                                fileRecord);
                            break;

                        case HfsCatalogDataRecordType.Folder:
                            var folderRecord = new HfsFolderRecord(CatalogTree.BlockBuffer.Slice(dataOffset, HfsFolderRecord.Size));
                            yield return new HfsDirectory(
                                key.ParentIdentifier,
                                key.Name,
                                folderRecord);
                            break;

                        default:
                            // Ignore other record types for now.
                            break;

                    }
                }
            }

            if (currentNode.Value.Descriptor.NextNodeNumber == 0)
            {
                currentNode = null;
            }
            else
            {
                currentNode = CatalogTree.GetNode(currentNode.Value.Descriptor.NextNodeNumber);
            }
        }
    }

    private int ReadForkData(uint fileID, HfsExtentRecord firstExtents, HfsForkType forkType, uint dataSize, uint allocatedSize, Stream outputStream)
    {
        // NOTE: According to the HFS spec the block number fields in the file record
        // (DataForkBlockNumber / ResourceForkBlockNumber) are not used. The extents
        // describe the (allocation) blocks that contain the fork's data. Each extent
        // descriptor gives a starting allocation block and a block count. Blocks are
        // contiguous within an extent. Additional extents (beyond the first 3) are
        // stored in the extents overflow file.

        uint remaining = dataSize;
        int totalBytesWritten = 0;
        Span<byte> blockBuffer = stackalloc byte[(int)MasterDirectoryBlock.AllocationBlockSize];
        
        // Start with the first 3 extents from the file record
        HfsExtentRecord currentExtents = firstExtents;
        ushort totalBlocksRead = 0; // Track which allocation block we're at in the fork

        while (remaining > 0)
        {
            bool processedAnyExtent = false;

            int currentExtentBlocksRead = 0;
            
            // Process the current extent record (up to 3 extents)
            for (int extentIndex = 0; extentIndex < 3 && remaining > 0; extentIndex++)
            {
                var extent = currentExtents[extentIndex];
                if (extent.BlockCount == 0)
                {
                    continue; // Skip empty descriptors.
                }

                processedAnyExtent = true;

                var offset = MasterDirectoryBlock.ExtentsStartBlockNumber * 512 + extent.StartBlock * MasterDirectoryBlock.AllocationBlockSize;
                var size = extent.BlockCount * MasterDirectoryBlock.AllocationBlockSize;

                // Read the extent's blocks.
                if (_streamStartOffset + offset >= _stream.Length)
                {
                    throw new InvalidDataException($"Extent block {extent.StartBlock} at {offset} is out of bounds of the stream.");
                }

                _stream.Seek(_streamStartOffset + offset, SeekOrigin.Begin);

                for (int i = 0; i < extent.BlockCount; i++)
                {
                    if (_stream.Read(blockBuffer) != blockBuffer.Length)
                    {
                        throw new InvalidDataException("Unable to read full allocation block for file fork.");
                    }

                    int bytesToCopy = (int)Math.Min(remaining, blockBuffer.Length);
                    outputStream.Write(blockBuffer[..bytesToCopy]);
                    totalBytesWritten += bytesToCopy;
                    remaining -= (uint)bytesToCopy;
                    totalBlocksRead++;
                    currentExtentBlocksRead++;
                }
            }

            if (remaining > 0)
            {
                Debug.Assert(currentExtentBlocksRead == currentExtents.TotalBlockCount, "Did not read all data from the extents record.");

                // Need more extents - fetch from the extents overflow file
                var overflowExtents = GetExtentsFromOverflow(fileID, forkType, totalBlocksRead)
                    ?? throw new InvalidDataException($"Insufficient extent descriptors to satisfy declared fork size. Remaining: {remaining} bytes, StartBlock: {totalBlocksRead}");
                currentExtents = overflowExtents;
                processedAnyExtent = false; // Will process in next iteration
            }
            else if (!processedAnyExtent)
            {
                // No extents processed and no data remaining - we're done
                break;
            }
        }

        return totalBytesWritten;
    }

    /// <summary>
    /// Gets additional extents from the extents overflow B-tree.
    /// </summary>
    /// <param name="fileID">The file identifier.</param>
    /// <param name="forkType">The fork type (data or resource).</param>
    /// <param name="startBlock">The starting allocation block number to search for.</param>
    /// <returns>The extent record if found; otherwise, null.</returns>
    private HfsExtentRecord? GetExtentsFromOverflow(uint fileID, HfsForkType forkType, ushort startBlock)
    {
        if (ExtentsOverflowTree == null)
        {
            return null;
        }

        var comparison = new HfsExtentsKeyComparison(fileID, forkType, startBlock);
        var currentNode = ExtentsOverflowTree.FindFirstMatchingLeafNode(comparison);
        while (currentNode != null)
        {
            // Search the leaf node for the matching extent record
            for (int i = 0; i < currentNode.Value.Descriptor.RecordCount; i++)
            {
                var recordOffset = currentNode.Value.RecordOffsets[i];
                var extentKey = new HfsExtentsKey(ExtentsOverflowTree.BlockBuffer.Slice(recordOffset.Offset, HfsExtentsKey.Size));

                if (extentKey.ForkType == forkType && extentKey.FileID == fileID && extentKey.StartBlock == startBlock)
                {
                    // Found the matching extent record - it follows the key
                    var extentDataOffset = recordOffset.Offset + HfsExtentsKey.Size;
                    return new HfsExtentRecord(ExtentsOverflowTree.BlockBuffer.Slice(extentDataOffset, HfsExtentRecord.Size));
                }
                
                // Keys are sorted, so if we've passed our target, stop searching
                if (extentKey.CompareTo(comparison) > 0)
                {
                    break;
                }
            }

            if (currentNode.Value.Descriptor.NextNodeNumber == 0)
            {
                currentNode = null;
            }
            else
            {
                currentNode = CatalogTree.GetNode(currentNode.Value.Descriptor.NextNodeNumber);
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the data fork of a file as a byte array.
    /// </summary>
    /// <param name="file">The file to read.</param>
    /// <returns>The data fork data as a byte array.</returns>
    public byte[] GetDataForkData(HfsFile file)
    {
        return GetFileData(file, HfsForkType.DataFork);
    }

    /// <summary>
    /// Writes the data fork of a file to the specified output stream.
    /// </summary>
    /// <param name="file">The file to read.</param>
    /// <param name="outputStream">The stream to write the file data to.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public int GetDataForkData(HfsFile file, Stream outputStream)
    {
        return GetFileData(file, outputStream, HfsForkType.DataFork);
    }

    /// <summary>
    /// Gets the resource fork of a file as a byte array.
    /// </summary>
    /// <param name="file">The file to read.</param>
    /// <returns>The resource fork data as a byte array.</returns>
    public byte[] GetResourceForkData(HfsFile file)
    {
        return GetFileData(file, HfsForkType.ResourceFork);
    }

    /// <summary>
    /// Writes the resource fork of a file to the specified output stream.
    /// </summary>
    /// <param name="file">The file to read.</param>
    /// <param name="outputStream">The stream to write the file data to.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public int GetResourceForkData(HfsFile file, Stream outputStream)
    {
        return GetFileData(file, outputStream, HfsForkType.ResourceFork);
    }

    /// <summary>
    /// Gets the data of a file as a byte array.
    /// </summary>
    /// <param name="file">The file to read.</param>
    /// <param name="forkType">The fork type (data or resource).</param>
    /// <returns>The file data as a byte array.</returns>
    public byte[] GetFileData(HfsFile file, HfsForkType forkType)
    {
        using var ms = new MemoryStream();
        GetFileData(file, ms, forkType);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes the data of a file to the specified output stream.
    /// </summary>
    /// <param name="file">The file to read.</param>
    /// <param name="outputStream">The stream to write the file data to.</param>
    /// <param name="forkType">The fork type (data or resource).</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public int GetFileData(HfsFile file, Stream outputStream, HfsForkType forkType)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(outputStream);
        
        if (!Enum.IsDefined(typeof(HfsForkType), forkType))
        {
            throw new ArgumentOutOfRangeException(nameof(forkType), "Invalid fork type specified.");
        }

        if (forkType == HfsForkType.DataFork)
        {
            return ReadForkData(
                file.FileRecord.Identifier,
                file.FileRecord.FirstDataForkExtents,
                HfsForkType.DataFork,
                file.FileRecord.DataForkSize,
                file.FileRecord.DataForkAllocatedSize,
                outputStream);
        }
        else
        {
            return ReadForkData(
                file.FileRecord.Identifier,
                file.FileRecord.FirstResourceForkExtents,
                HfsForkType.ResourceFork,
                file.FileRecord.ResourceForkSize,
                file.FileRecord.ResourceForkAllocatedSize,
                outputStream);
        }
    }
}
