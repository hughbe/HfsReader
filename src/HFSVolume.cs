using HfsReader.Utilities;

namespace HfsReader;

public class HFSVolume
{
    private readonly Stream _stream;
    private readonly int _streamStartOffset;

    public HFSBootBlockHeader BootBlock { get; }
    public HFSMasterDirectoryBlock MasterDirectoryBlock { get; }
    public BTree CatalogTree { get; }

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
    }


    public IEnumerable<HFSNode> RootContents() => ContentsOfDirectory((uint)HFSKnownCatalogNodeID.kHFSRootParentID);

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
                else if (key.ParentIdentifier == parentIdentifier)
                {
                    var type = (HFSCatalogDataRecordType)SpanUtilities.ReadUInt16BE(CatalogTree.BlockBuffer, dataOffset);
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

                    var index = SpanUtilities.ReadUInt32BE(CatalogTree.BlockBuffer, recordOffset.Offset + indexKey.KeySize + 1);

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

    private int ReadBlock(HFSExtentRecord extents, ushort blockNumber, uint dataSize, uint allocatedSize, Stream outputStream)
    {
        // NOTE: According to the HFS spec the block number fields in the file record
        // (DataForkBlockNumber / ResourceForkBlockNumber) are not used. The extents
        // describe the (allocation) blocks that contain the fork's data. Each extent
        // descriptor gives a starting allocation block and a block count. Blocks are
        // contiguous within an extent. Additional extents (beyond the first 3) would
        // be stored in the extents overflow file – currently not implemented here.
        // We therefore only read the first extent record.

        // Sanity: if the first extent record does not describe enough blocks to cover the
        // allocated size, the remaining extents must be fetched from the Extents Overflow file.
        int describedBlocks = 0;
        for (int i = 0; i < 3; i++)
        {
            var d = extents[i];
            if (d.BlockCount == 0) continue; // Unused descriptor.
            describedBlocks += d.BlockCount;
        }

        // Allocated size is in bytes; convert for comparison (avoid division rounding issues).
        if (allocatedSize != 0 && (ulong)describedBlocks * MasterDirectoryBlock.AllocationBlockSize < allocatedSize)
        {
            // More extents exist in the Extents Overflow file; not implemented yet.
            throw new NotImplementedException("Extents overflow not handled yet (file has more than 3 extents).");
        }

        uint remaining = dataSize;
        int totalBytesWritten = 0;
        Span<byte> blockBuffer = stackalloc byte[(int)MasterDirectoryBlock.AllocationBlockSize];

        // Iterate each extent then each block within the extent until we've read dataSize bytes.
        for (int extentIndex = 0; extentIndex < 3 && remaining > 0; extentIndex++)
        {
            var extent = extents[extentIndex];
            if (extent.BlockCount == 0)
            {
                continue; // Skip empty descriptors.
            }

            for (int blockIndex = 0; blockIndex < extent.BlockCount && remaining > 0; blockIndex++)
            {
                ulong absoluteBlockNumber = MasterDirectoryBlock.ExtentsStartBlockNumber + (ulong)extent.StartBlock + (ulong)blockIndex; // Allocation block number relative to start of volume.
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
            }
        }

        if (remaining > 0)
        {
            throw new InvalidDataException("Insufficient extent descriptors to satisfy declared fork size.");
        }

        return totalBytesWritten;
    }

    public byte[] GetFileData(HFSFile file, bool resourceFork)
    {
        using var ms = new MemoryStream();
        GetFileData(file, ms, resourceFork);
        return ms.ToArray();
    }

    public int GetFileData(HFSFile file, Stream outputStream, bool resourceFork)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(outputStream);

        if (!resourceFork)
        {
            return ReadBlock(file.FileRecord.FirstDataForkExtents, file.FileRecord.DataForkBlockNumber, file.FileRecord.DataForkSize, file.FileRecord.DataForkAllocatedSize, outputStream);
        }
        else
        {
            return ReadBlock(file.FileRecord.FirstResourceForkExtents, file.FileRecord.ResourceForkBlockNumber, file.FileRecord.ResourceForkSize, file.FileRecord.ResourceForkAllocatedSize, outputStream);
        }
    }
}
