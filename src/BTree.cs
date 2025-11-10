namespace HfsReader;

public class BTree
{
    private readonly Stream _stream;
    private readonly int _streamStartOffset;
    private readonly HFSExtentRecord _extents;
    private readonly int _extentsStartBlock;
    private readonly int _allocationBlockSize;

    public BTHeaderRec Header { get; }
    public BTNode RootNode => GetNode(Header.RootNodeNumber);

    private readonly byte[] _blockBuffer;
    public Span<byte> BlockBuffer => _blockBuffer;

    public BTree(Stream stream, int streamStartOffset, HFSExtentRecord extents, int extentsStartBlock, uint allocationBlockSize)
    {
        _stream = stream;
        _streamStartOffset = streamStartOffset;
        _extents = extents; 
        _extentsStartBlock = extentsStartBlock;
        _allocationBlockSize = (int)allocationBlockSize;

        // Read the header node (always at index 0)
        _blockBuffer = new byte[_allocationBlockSize];
        var headerNode = GetNode(0);

        if (headerNode.Descriptor.NodeType != BTNodeType.HeaderNode
            || headerNode.Descriptor.NodeLevel != 0
            || headerNode.Descriptor.PreviousNodeNumber != 0)
        {
            throw new InvalidDataException("Expected catalog B-tree header node.");
        }

        if (headerNode.Descriptor.RecordCount < 3)
        {
            throw new InvalidDataException($"B-tree header needs at least 3 records, found {headerNode.Descriptor.RecordCount}.");
        }

        var headerRecordOffset = headerNode.RecordOffsets[0];
        if (headerRecordOffset.Size < BTHeaderRec.Size)
        {
            throw new InvalidDataException($"B-tree header record should be {BTHeaderRec.Size} bytes, found {headerRecordOffset.Size} bytes.");
        }

        Header = new BTHeaderRec(BlockBuffer.Slice(headerRecordOffset.Offset, headerRecordOffset.Size));
    }

    public BTNode GetNode(uint nodeIndex)
    {
        var offset = GetNodeFileOffset(nodeIndex);
        _stream.Seek(offset, SeekOrigin.Begin);

        if (_stream.Read(_blockBuffer) != _blockBuffer.Length)
        {
            throw new InvalidDataException("Unable to read catalog node.");
        }

        return new BTNode(nodeIndex, _blockBuffer);
    }

    private int GetNodeFileOffset(uint nodeIndex)
    {
        HFSExtentDescriptor? extent = null;
        uint remainingNodeIndex = nodeIndex;

        for (int i = 0; i < 3; i++)
        {
            var currentExtent = _extents[i];
            if (remainingNodeIndex < currentExtent.BlockCount)
            {
                extent = currentExtent;
                break;
            }

            remainingNodeIndex -= currentExtent.BlockCount;
        }

        if (extent == null)
        {
            throw new InvalidDataException("Catalog node index is out of range of the catalog file extents.");
        }

        // The extents are in allocation blocks, so calculate the file offset accordingly.
        var fileOffset = (int)((_extentsStartBlock * 512) + (extent.Value.StartBlock + remainingNodeIndex) * _allocationBlockSize);
        
        // If the volume is embedded within a larger stream (e.g., a partition),
        // adjust the offset accordingly.
        return _streamStartOffset + fileOffset;
    }
}
