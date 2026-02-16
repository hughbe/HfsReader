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

    /// <summary>
    /// Gets the depth of the tree.
    /// </summary>
    public ushort Depth { get; }

    /// <summary>
    /// Gets the root node number.
    /// </summary>
    public uint RootNodeNumber { get; }

    /// <summary>
    /// Gets the number of data records contained in leaf nodes.
    /// </summary>
    public uint LeafRecords { get; }

    /// <summary>
    /// Gets the first leaf node number.
    /// </summary>
    public uint FirstLeafNodeNumber { get; }

    /// <summary>
    /// Gets the last leaf node number.
    /// </summary>
    public uint LastLeafNodeNumber { get; }

    /// <summary>
    /// Gets the node size in bytes.
    /// </summary>
    public ushort NodeSize { get; }

    /// <summary>
    /// Gets the maximum key size in bytes.
    /// </summary>
    public ushort MaximumKeySize { get; }

    /// <summary>
    /// Gets the total number of nodes.
    /// </summary>
    public uint TotalNodes { get; }

    /// <summary>
    /// Gets the number of free nodes.
    /// </summary>
    public uint FreeNodes { get; }

    /// <summary>
    /// Gets the reserved bytes (76 bytes).
    /// </summary>
    public fixed byte Reserved[76];

    /// <summary>
    /// Initializes a new instance of the <see cref="BTHeaderRec"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the B-tree header record data.</param>
    /// <exception cref="ArgumentException">Thrown when the data length is not equal to the required size.</exception>
    public BTHeaderRec(Span<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"B-tree header record data must be exactly {Size} bytes long.", nameof(data));
        }

        // Structure documented in https://developer.apple.com/library/archive/technotes/tn/tn1150.html
        int offset = 0;

        // The current depth of the B-tree. Always equal to the height field of the root node.
        Depth = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // The node number of the root node, the index node that acts as the root of
        // the B-tree. See Index Nodes for details. There is a possibility that the
        // rootNode is a leaf node. See Inside Macintosh: Files, pp. 2-69 for details.
        RootNodeNumber = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // The total number of records contained in all of the leaf nodes.
        LeafRecords = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // The node number of the first leaf node. This may be zero if there are no leaf nodes.
        FirstLeafNodeNumber = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // The node number of the last leaf node. This may be zero if there are no leaf nodes.
        LastLeafNodeNumber = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // The size, in bytes, of a node. This is a power of two, from 512 through 32,768, inclusive.
        NodeSize = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // The maximum length of a key in an index or leaf node. HFSVolumes.h has
        // the maxKeyLength values for the catalog and extents files for both HFS
        // and HFS Plus (kHFSPlusExtentKeyMaximumLength, kHFSExtentKeyMaximumLength,
        // kHFSPlusCatalogKeyMaximumLength, kHFSCatalogKeyMaximumLength). The maximum
        // key length for the attributes B-tree will probably be a little larger
        // than for the catalog file. In general, maxKeyLength has to be small enough (compared to nodeSize) so that a single node can fit two keys of maximum size plus the node descriptor and offsets.
        MaximumKeySize = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // The total number of nodes (be they free or used) in the B-tree.
        // The length of the B-tree file is this value times the nodeSize.
        TotalNodes = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // The number of unused nodes in the B-tree.
        FreeNodes = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Reserved (76 bytes)
        fixed (byte* reservedPtr = Reserved)
        {
            data.Slice(offset, 76).CopyTo(new Span<byte>(reservedPtr, 76));
            offset += 76;
        }

        Debug.Assert(offset == data.Length, "Did not read the expected number of bytes for BTHeaderRec.");
    }
}
