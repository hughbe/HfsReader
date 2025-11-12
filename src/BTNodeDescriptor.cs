using System.Buffers.Binary;
using System.Diagnostics;

namespace HfsReader;

public struct BTNodeDescriptor
{
    public uint NextNodeNumber { get; }

    public uint PreviousNodeNumber { get; }

    public BTNodeType NodeType { get; }

    public sbyte NodeLevel { get; }

    public ushort RecordCount { get; }

    public ushort Reserved { get; }

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

public enum BTNodeType : sbyte
{
    LeafNode = -1,
    IndexNode = 0,
    HeaderNode = 1,
    MapNode = 2
}
