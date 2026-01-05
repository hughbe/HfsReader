namespace HfsReader;

/// <summary>
/// Represents a B-tree structure used in HFS for catalog and extents files.
/// </summary>
public class BTree
{
    private readonly Stream _stream;
    private readonly int _streamStartOffset;
    private readonly HFSExtentRecord _extents;
    private readonly int _extentsStartBlock;
    private readonly int _allocationBlockSize;
    private readonly int _nodeSize;

    /// <summary>
    /// Gets the header record of the B-tree.
    /// </summary>
    public BTHeaderRec Header { get; }

    /// <summary>
    /// Gets the root node of the B-tree.
    /// </summary>
    public BTNode RootNode => GetNode(Header.RootNodeNumber);

    private readonly byte[] _blockBuffer;
    /// <summary>
    /// Gets the buffer containing the current block's data.
    /// </summary>
    public Span<byte> BlockBuffer => _blockBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="BTree"/> class.
    /// </summary>
    /// <param name="stream">The stream containing the B-tree data.</param>
    /// <param name="streamStartOffset">The start offset of the stream.</param>
    /// <param name="extents">The extents describing the B-tree's location.</param>
    /// <param name="extentsStartBlock">The starting block of the extents.</param>
    /// <param name="allocationBlockSize">The size of an allocation block.</param>
    public BTree(Stream stream, int streamStartOffset, HFSExtentRecord extents, int extentsStartBlock, uint allocationBlockSize)
    {
        _stream = stream;
        _streamStartOffset = streamStartOffset;
        _extents = extents; 
        _extentsStartBlock = extentsStartBlock;
        _allocationBlockSize = (int)allocationBlockSize;

        // First, read the header node with a temporary 512-byte buffer (minimum node size)
        // The header node is always at index 0 (beginning of the catalog file).
        Span<byte> tempBuffer = stackalloc byte[512];
        
        var catalogFileOffset = GetCatalogFileOffset();
        _stream.Seek(catalogFileOffset, SeekOrigin.Begin);
        if (_stream.Read(tempBuffer) != tempBuffer.Length)
        {
            throw new InvalidDataException("Unable to read catalog header node.");
        }
        
        var headerNode = new BTNode(0, tempBuffer);

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

        Header = new BTHeaderRec(tempBuffer.Slice(headerRecordOffset.Offset, headerRecordOffset.Size));
        
        // Now update the node size from the header
        _nodeSize = Header.NodeSize;
        _blockBuffer = new byte[_nodeSize];
    }
    
    /// <summary>
    /// Gets the offset of the catalog file within the stream.
    /// </summary>
    private int GetCatalogFileOffset()
    {
        // The catalog file starts at the first extent
        var extent = _extents[0];
        return _streamStartOffset + (_extentsStartBlock * 512) + (extent.StartBlock * _allocationBlockSize);
    }

    /// <summary>
    /// Gets the node at the specified index in the B-tree.
    /// </summary>
    /// <param name="nodeIndex">The index of the node to retrieve.</param>
    /// <returns>The <see cref="BTNode"/> at the specified index.</returns>
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
        // Calculate the byte offset of the node within the catalog file.
        // Each node is _nodeSize bytes (typically 512 for HFS).
        int nodeByteOffset = (int)(nodeIndex * _nodeSize);
        
        // Find which extent and allocation block contains this byte offset.
        int currentByteOffset = 0;
        
        for (int i = 0; i < 3; i++)
        {
            var currentExtent = _extents[i];
            if (currentExtent.BlockCount == 0)
            {
                break;
            }
            
            int extentBytes = currentExtent.BlockCount * _allocationBlockSize;
            
            if (nodeByteOffset < currentByteOffset + extentBytes)
            {
                // The node is in this extent
                int offsetWithinExtent = nodeByteOffset - currentByteOffset;
                int absoluteOffset = _streamStartOffset + 
                    (_extentsStartBlock * 512) + 
                    (currentExtent.StartBlock * _allocationBlockSize) + 
                    offsetWithinExtent;
                return absoluteOffset;
            }
            
            currentByteOffset += extentBytes;
        }

        throw new InvalidDataException($"Catalog node index {nodeIndex} is out of range of the catalog file extents.");
    }
}
