using System.Buffers.Binary;
using System.Diagnostics;

namespace HfsReader;

/// <summary>
/// Represents a B-tree header record.
/// </summary>
public unsafe struct BTHeaderRec
{
    /// <summary>
    /// The size of the B-tree header record in bytes.
    /// </summary>
    public const int Size = 106;

    /// <summary>Gets the depth of the tree.</summary>
    public ushort TreeDepth { get; }

    /// <summary>Gets the root node number.</summary>
    public uint RootNodeNumber { get; }

    /// <summary>Gets the number of data records contained in leaf nodes.</summary>
    public uint NumberOfDataRecords { get; }

    /// <summary>Gets the first leaf node number.</summary>
    public uint FirstLeafNodeNumber { get; }

    /// <summary>Gets the last leaf node number.</summary>
    public uint LastLeafNodeNumber { get; }

    /// <summary>Gets the node size in bytes.</summary>
    public ushort NodeSize { get; }

    /// <summary>Gets the maximum key size in bytes.</summary>
    public ushort MaximumKeySize { get; }

    /// <summary>Gets the number of nodes.</summary>
    public uint NumberOfNodes { get; }

    /// <summary>Gets the number of free nodes.</summary>
    public uint NumberOfFreeNodes { get; }

    /// <summary>Gets the reserved bytes (76 bytes).</summary>
    public fixed byte Reserved[76];

    /// <summary>
    /// Initializes a new instance of the <see cref="BTHeaderRec"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the B-tree header record data.</param>
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
