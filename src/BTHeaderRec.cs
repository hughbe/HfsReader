using System.Buffers.Binary;
using System.Diagnostics;

namespace HfsReader;

public unsafe struct BTHeaderRec
{
    public const int Size = 106;

    public ushort TreeDepth { get; }

    public uint RootNodeNumber { get; }

    public uint NumberOfDataRecords { get; }

    public uint FirstLeafNodeNumber { get; }

    public uint LastLeafNodeNumber { get; }

    public ushort NodeSize { get; }

    public ushort MaximumKeySize { get; }

    public uint NumberOfNodes { get; }

    public uint NumberOfFreeNodes { get; }

    public fixed byte Reserved[76];

    public BTHeaderRec(Span<byte> data)
    {
        if (data.Length < Size)
        {
            throw new ArgumentException("BTHeaderRec data must be at least 106 bytes long.", nameof(data));
        }

        int offset = 0;

        // Depth of the tree
        TreeDepth = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        // Root node number
        RootNodeNumber = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Number of data records contained in leaf nodes
        // (Does this equals the number of leaf nodes?)
        NumberOfDataRecords = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // First leaf node number
        FirstLeafNodeNumber = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Last leaf node number
        LastLeafNodeNumber = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // The node size
        // Contains number of bytes
        NodeSize = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        // Maximum key size
        // Contains number of bytes
        MaximumKeySize = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        // Number of nodes
        NumberOfNodes = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Number of free nodes
        NumberOfFreeNodes = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Reserved (76 bytes)
        fixed (byte* reservedPtr = Reserved)
        {
            data.Slice(offset, 76).CopyTo(new Span<byte>(reservedPtr, 76));
            offset += 76;
        }

        Debug.Assert(offset == Size);
    }
}
