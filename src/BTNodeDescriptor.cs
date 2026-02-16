using System.Buffers.Binary;
using System.Diagnostics;

namespace HfsReader;

/// <summary>
/// Represents a B-tree node descriptor.
/// </summary>
public readonly struct BTNodeDescriptor
{
    /// <summary>
    /// Gets the size of a B-tree node descriptor.
    /// </summary>
    public const int Size = 14;

    /// <summary>
    /// Gets the next tree node number (forward link).
    /// </summary>
    public uint NextNodeNumber { get; }

    /// <summary>
    /// Gets the previous tree node number (backward link).
    /// </summary>
    public uint PreviousNodeNumber { get; }

    /// <summary>
    /// Gets the node kind.
    /// </summary>
    public BTNodeKind Kind { get; }

    /// <summary>
    /// Gets the node level (0 for root, maximum depth of 8).
    /// </summary>
    public sbyte Level { get; }

    /// <summary>
    /// Gets the number of records in the node.
    /// </summary>
    public ushort RecordCount { get; }

    /// <summary>
    /// Gets the reserved field.
    /// </summary>
    public ushort Reserved { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BTNodeDescriptor"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the node descriptor data.</param>
    /// <exception cref="ArgumentException">Thrown when the data length is less than the required size.</exception>
    public BTNodeDescriptor(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Node descriptor data must be exactly {Size} bytes long.", nameof(data));
        }

        // Structure documented in https://developer.apple.com/library/archive/technotes/tn/tn1150.html#BTrees
        int offset = 0;

        // The node number of the next node of this type, or 0 if this is the last node.
        NextNodeNumber = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // The node number of the previous node of this type, or 0 if this is the first node.
        PreviousNodeNumber = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // The type of this node. There are four node kinds, defined by the constants listed below.
        Kind = (BTNodeKind)data[offset];
        offset += 1;

        // The level, or depth, of this node in the B-tree hierarchy. For the header node,
        // this field must be zero. For leaf nodes, this field must be one. For index nodes,
        // this field is one greater than the height of the child nodes it points to.
        // The height of a map node is zero, just like for a header node. (Think of map
        // nodes as extensions of the map record in the header node.)
        Level = (sbyte)data[offset];
        offset += 1;

        // The number of records contained in this node.
        RecordCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // An implementation must treat this as a reserved field.
        Reserved = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        Debug.Assert(offset == data.Length, "Did not read the expected number of bytes for BTNodeDescriptor.");
    }
}
