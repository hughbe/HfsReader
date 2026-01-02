using System.Buffers.Binary;

namespace HfsReader;

/// <summary>
/// Represents a Hierarchical File System (HFS) volume and provides access to its contents.
/// </summary>
public class HFSVolume
{
    private readonly Stream _stream;
    private readonly int _streamStartOffset;

    /// <summary>
    /// Gets the boot block header of the HFS volume.
    /// </summary>
    public HFSBootBlockHeader BootBlock { get; }

    /// <summary>
    /// Gets the master directory block of the HFS volume.
    /// </summary>
    public HFSMasterDirectoryBlock MasterDirectoryBlock { get; }

    /// <summary>
    /// Gets the catalog B-tree of the HFS volume.
    /// </summary>
    public BTree CatalogTree { get; }

    /// <summary>
    /// Gets the extents overflow B-tree of the HFS volume.
    /// </summary>
    public BTree? ExtentsOverflowTree { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HFSVolume"/> class.
    /// </summary>
    /// <param name="stream">The stream containing the HFS volume data.</param>
    /// <param name="volumeStartOffset">The start offset of the volume within the stream.</param>
    public HFSVolume(Stream stream, int volumeStartOffset)
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

        MasterDirectoryBlock = new HFSMasterDirectoryBlock(blockBuffer);

        // Initialize the catalog B-tree
        CatalogTree = new BTree(_stream, _streamStartOffset, MasterDirectoryBlock.CatalogFileExtents, MasterDirectoryBlock.ExtentsStartBlockNumber, MasterDirectoryBlock.AllocationBlockSize);

        // Initialize the extents overflow B-tree if it exists
        if (MasterDirectoryBlock.ExtentsOverflowFileSize > 0)
        {
            ExtentsOverflowTree = new BTree(_stream, _streamStartOffset, MasterDirectoryBlock.ExtentsOverflowExtents, MasterDirectoryBlock.ExtentsStartBlockNumber, MasterDirectoryBlock.AllocationBlockSize);
        }
    }

    /// <summary>
    /// Gets the contents of the root directory of the HFS volume.
    /// </summary>
    /// <returns>An enumerable of <see cref="HFSNode"/> objects in the root directory.</returns>
    public IEnumerable<HFSNode> RootContents() => ContentsOfDirectory((uint)HFSKnownCatalogNodeID.kHFSRootParentID);

    /// <summary>
    /// Gets the contents of the specified directory.
    /// </summary>
    /// <param name="directory">The directory whose contents to retrieve.</param>
    /// <returns>An enumerable of <see cref="HFSNode"/> objects in the directory.</returns>
    public IEnumerable<HFSNode> ContentsOfDirectory(HFSDirectory directory)
    {
        ArgumentNullException.ThrowIfNull(directory);
        return [.. ContentsOfDirectory(directory.Identifier)];
    }

    private IEnumerable<HFSNode> ContentsOfDirectory(uint parentIdentifier)
    {
        var currentNode = FindFirstMatchingLeafNode(parentIdentifier, string.Empty);
        while (currentNode != null)
        {
            for (int i = 0; i < currentNode.Value.Descriptor.RecordCount; i++)
            {
                var recordOffset = currentNode.Value.RecordOffsets[i];
                var key = new HFSCatalogIndexKey(CatalogTree.BlockBuffer.Slice(recordOffset.Offset, recordOffset.Size));

                // Data records are placed immediately after the key length byte and key data,
                // then padded to an even boundary. The key length does NOT include the length byte.
                var dataOffset = recordOffset.Offset + 1 + key.KeySize;
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
                    var type = (HFSCatalogDataRecordType)BinaryPrimitives.ReadUInt16BigEndian(CatalogTree.BlockBuffer[dataOffset..]);
                    switch (type)
                    {
                        case HFSCatalogDataRecordType.File:
                            var fileRecord = new HFSFileRecord(CatalogTree.BlockBuffer.Slice(dataOffset, recordOffset.Size - (dataOffset - recordOffset.Offset)));
                            yield return new HFSFile(
                                key.ParentIdentifier,
                                key.Name ?? string.Empty,
                                fileRecord);
                            break;

                        case HFSCatalogDataRecordType.Folder:
                            var folderRecord = new HFSFolderRecord(CatalogTree.BlockBuffer.Slice(dataOffset, recordOffset.Size - (dataOffset - recordOffset.Offset)));
                            yield return new HFSDirectory(
                                key.ParentIdentifier,
                                key.Name ?? string.Empty,
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

    /// <summary>
    /// Find the first leaf node that contains entries for the given parent identifier.
    /// According to the HFS spec, catalog entries are sorted first by parent ID, then by name.
    /// </summary>
    private BTNode? FindFirstMatchingLeafNode(uint parentIdentifier, string name)
    {
        BTNode currentNode = CatalogTree.RootNode;

        while (currentNode.Descriptor.NodeType != BTNodeType.LeafNode)
        {
            if (currentNode.Descriptor.NodeType == BTNodeType.IndexNode)
            {
                uint? nextNodeIndex = null;
                for (int i = 0; i < currentNode.Descriptor.RecordCount; i++)
                {
                    var recordOffset = currentNode.RecordOffsets[i];
                    var indexKey = new HFSCatalogIndexKey(CatalogTree.BlockBuffer.Slice(recordOffset.Offset, recordOffset.Size));

                    var index = BinaryPrimitives.ReadUInt32BigEndian(CatalogTree.BlockBuffer[(recordOffset.Offset + indexKey.KeySize + 1)..]);

                    if (indexKey.CompareTo(parentIdentifier, name) > 0)
                    {
                        // If the current index key is greater than the target parent ID and file name,
                        // stop.
                        // But, this isn't true if we have reached the first matching parent
                        // ID but the name is empty (we want the first entry for that parent
                        // ID).
                        if (nextNodeIndex == null && string.IsNullOrEmpty(name) && indexKey.ParentIdentifier == parentIdentifier)
                        {
                            nextNodeIndex = index;
                        }

                        break;
                    }
                    else
                    {
                        nextNodeIndex = index;
                    }
                }

                if (nextNodeIndex != null)
                {
                    currentNode = CatalogTree.GetNode(nextNodeIndex.Value);
                }
                else
                {
                    return null;
                }
            }
        }

        return currentNode;
    }

    private int ReadForkData(uint fileID, HFSExtentRecord firstExtents, HFSForkType forkType, uint dataSize, uint allocatedSize, Stream outputStream)
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
        HFSExtentRecord currentExtents = firstExtents;
        ushort currentStartBlock = 0; // Track which allocation block we're at in the fork

        while (remaining > 0)
        {
            bool processedAnyExtent = false;
            
            // Process the current extent record (up to 3 extents)
            for (int extentIndex = 0; extentIndex < 3 && remaining > 0; extentIndex++)
            {
                var extent = currentExtents[extentIndex];
                if (extent.BlockCount == 0)
                {
                    continue; // Skip empty descriptors.
                }

                processedAnyExtent = true;

                for (int blockIndex = 0; blockIndex < extent.BlockCount && remaining > 0; blockIndex++)
                {
                    ulong absoluteBlockNumber = MasterDirectoryBlock.ExtentsStartBlockNumber + (ulong)extent.StartBlock + (ulong)blockIndex;
                    long seekOffset = (long)absoluteBlockNumber * (long)MasterDirectoryBlock.AllocationBlockSize;
                    _stream.Seek(_streamStartOffset + seekOffset, SeekOrigin.Begin);

                    // Read a full allocation block, then copy only the required bytes from its start.
                    int readBytes = _stream.Read(blockBuffer);
                    if (readBytes != (int)MasterDirectoryBlock.AllocationBlockSize)
                    {
                        throw new InvalidDataException("Unable to read full allocation block for file fork.");
                    }

                    int bytesToCopy = (int)Math.Min(remaining, MasterDirectoryBlock.AllocationBlockSize);
                    outputStream.Write(blockBuffer[..bytesToCopy]);
                    totalBytesWritten += bytesToCopy;
                    remaining -= (uint)bytesToCopy;
                    currentStartBlock++;
                }
            }

            if (remaining > 0)
            {
                // Need more extents - fetch from the extents overflow file
                var overflowExtents = GetExtentsFromOverflow(fileID, forkType, currentStartBlock);
                if (overflowExtents == null)
                {
                    throw new InvalidDataException($"Insufficient extent descriptors to satisfy declared fork size. Remaining: {remaining} bytes, StartBlock: {currentStartBlock}");
                }
                currentExtents = overflowExtents.Value;
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
    private HFSExtentRecord? GetExtentsFromOverflow(uint fileID, HFSForkType forkType, ushort startBlock)
    {
        if (ExtentsOverflowTree == null)
        {
            return null;
        }

        // Search the extents overflow B-tree for the matching extent record
        BTNode currentNode = ExtentsOverflowTree.RootNode;

        // Navigate to the leaf node
        while (currentNode.Descriptor.NodeType != BTNodeType.LeafNode)
        {
            if (currentNode.Descriptor.NodeType == BTNodeType.IndexNode)
            {
                uint? nextNodeIndex = null;
                for (int i = 0; i < currentNode.Descriptor.RecordCount; i++)
                {
                    var recordOffset = currentNode.RecordOffsets[i];
                    var extentKey = new HFSExtentKey(ExtentsOverflowTree.BlockBuffer.Slice(recordOffset.Offset, HFSExtentKey.Size));
                    
                    // The index record contains a pointer to the child node after the key
                    var index = BinaryPrimitives.ReadUInt32BigEndian(ExtentsOverflowTree.BlockBuffer[(recordOffset.Offset + HFSExtentKey.Size)..]);

                    if (extentKey.CompareTo(fileID, forkType, startBlock) > 0)
                    {
                        break;
                    }
                    nextNodeIndex = index;
                }

                if (nextNodeIndex != null)
                {
                    currentNode = ExtentsOverflowTree.GetNode(nextNodeIndex.Value);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        // Search the leaf node for the matching extent record
        for (int i = 0; i < currentNode.Descriptor.RecordCount; i++)
        {
            var recordOffset = currentNode.RecordOffsets[i];
            var extentKey = new HFSExtentKey(ExtentsOverflowTree.BlockBuffer.Slice(recordOffset.Offset, HFSExtentKey.Size));

            if (extentKey.ForkType == forkType && extentKey.FileID == fileID && extentKey.StartBlock == startBlock)
            {
                // Found the matching extent record - it follows the key
                var extentDataOffset = recordOffset.Offset + HFSExtentKey.Size;
                return new HFSExtentRecord(ExtentsOverflowTree.BlockBuffer.Slice(extentDataOffset, HFSExtentRecord.Size));
            }
            
            // Keys are sorted, so if we've passed our target, stop searching
            if (extentKey.CompareTo(fileID, forkType, startBlock) > 0)
            {
                break;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the data of a file as a byte array.
    /// </summary>
    /// <param name="file">The file to read.</param>
    /// <param name="resourceFork">True to read the resource fork; otherwise, false for the data fork.</param>
    /// <returns>The file data as a byte array.</returns>
    public byte[] GetFileData(HFSFile file, bool resourceFork)
    {
        using var ms = new MemoryStream();
        GetFileData(file, ms, resourceFork);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes the data of a file to the specified output stream.
    /// </summary>
    /// <param name="file">The file to read.</param>
    /// <param name="outputStream">The stream to write the file data to.</param>
    /// <param name="resourceFork">True to read the resource fork; otherwise, false for the data fork.</param>
    /// <returns>The number of bytes written to the output stream.</returns>
    public int GetFileData(HFSFile file, Stream outputStream, bool resourceFork)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(outputStream);

        if (!resourceFork)
        {
            return ReadForkData(
                file.FileRecord.Identifier,
                file.FileRecord.FirstDataForkExtents,
                HFSForkType.DataFork,
                file.FileRecord.DataForkSize,
                file.FileRecord.DataForkAllocatedSize,
                outputStream);
        }
        else
        {
            return ReadForkData(
                file.FileRecord.Identifier,
                file.FileRecord.FirstResourceForkExtents,
                HFSForkType.ResourceFork,
                file.FileRecord.ResourceForkSize,
                file.FileRecord.ResourceForkAllocatedSize,
                outputStream);
        }
    }
}
