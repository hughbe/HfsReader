using System.Buffers.Binary;
using System.Diagnostics;

namespace HfsReader;

/// <summary>
/// Represents a B-tree node descriptor.
/// </summary>
public struct BTNodeDescriptor
{
    /// <summary>Gets the next tree node number (forward link).</summary>
    public uint NextNodeNumber { get; }

    /// <summary>Gets the previous tree node number (backward link).</summary>
    public uint PreviousNodeNumber { get; }

    /// <summary>Gets the node type.</summary>
    public BTNodeType NodeType { get; }

    /// <summary>Gets the node level (0 for root, maximum depth of 8).</summary>
    public sbyte NodeLevel { get; }

    /// <summary>Gets the number of records in the node.</summary>
    public ushort RecordCount { get; }

    /// <summary>Gets the reserved field.</summary>
    public ushort Reserved { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BTNodeDescriptor"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the node descriptor data.</param>
    public BTNodeDescriptor(Span<byte> data)
    {
        if (data.Length < 14)
        {
            throw new ArgumentException("BTNodeDescriptor data must be at least 14 bytes long.", nameof(data));
        }

        int offset = 0;

        // The next tree node number (forward link)
        // Contains 0 if empty
        NextNodeNumber = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // The previous tree node number (backward link)
        // Contains 0 if empty
        PreviousNodeNumber = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // The node type
        // Signed 8-bit integer
        // See section: B-tree node type
        NodeType = (BTNodeType)data[offset];
        offset += 1;

        // The node level
        // Signed 8-bit integer
        // The root node level is 0, with a maximum depth of 8.
        NodeLevel = (sbyte)data[offset];
        offset += 1;

        // The number of records
        RecordCount = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        // Unknown (Reserved)
        // Should contain 0-byte values
        Reserved = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        Debug.Assert(offset == 14);
    }
}

/// <summary>
/// Represents the type of a B-tree node.
/// </summary>
public enum BTNodeType : sbyte
{
    /// <summary>A leaf node containing data records.</summary>
    LeafNode = -1,
    /// <summary>An index node containing pointers to other nodes.</summary>
    IndexNode = 0,
    /// <summary>A header node containing B-tree metadata.</summary>
    HeaderNode = 1,
    /// <summary>A map node containing allocation information.</summary>
    MapNode = 2
}
