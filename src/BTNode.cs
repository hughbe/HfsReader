using System.Buffers.Binary;

namespace HfsReader;

/// <summary>
/// Represents a node in a B-tree structure.
/// </summary>
public readonly struct BTNode
{
    /// <summary>
    /// Gets the index of this node in the B-tree.
    /// </summary>
    public uint NodeIndex { get; }

    /// <summary>
    /// Gets the descriptor for this node.
    /// </summary>
    public BTNodeDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the array of record offsets in this node.
    /// </summary>
    public BTRecordOffset[] RecordOffsets { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BTNode"/> struct from the given data.
    /// </summary>
    /// <param name="nodeIndex">The index of the node in the B-tree.</param>
    /// <param name="data">The span containing the node data.</param>
    public BTNode(uint nodeIndex, Span<byte> data)
    {
        NodeIndex = nodeIndex;
        Descriptor = new BTNodeDescriptor(data[..BTNodeDescriptor.Size]);

        // Validate the descriptor.
        if (Descriptor.RecordCount >= 64)
        {
            throw new NotSupportedException($"BTNode at index {nodeIndex} has an invalid record count of {Descriptor.RecordCount}.");
        }

        // 3.1.3. The B-tree record offsets
        // The B-tree record offsets are an array of 16-bit integers relative from the
        // start of the B-tree node record. The first record offset is found at node
        // size - 2, e.g. 512 - 2 = 510, the second 2 bytes before that, e.g. 508, etc.
        // An additional record offset is added at the end to signify the start of the
        // free space.
        RecordOffsets = new BTRecordOffset[Descriptor.RecordCount + 1];

        // First, enumerate to get the offsets.
        for (int i = 0; i <= Descriptor.RecordCount; i++)
        {
            int entryOffset = 512 - 2 - (i * 2);
            RecordOffsets[i] = new BTRecordOffset
            {
                Offset = BinaryPrimitives.ReadUInt16BigEndian(data[entryOffset..])
            };
        }

        // Second, calculate the sizes.
        for (int i = 0; i < Descriptor.RecordCount; i++)
        {
            RecordOffsets[i] = new BTRecordOffset
            {
                Offset = RecordOffsets[i].Offset,
                Size = RecordOffsets[i + 1].Offset - RecordOffsets[i].Offset
            };
        }
    }
}
