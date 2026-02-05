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

        // Some disks have a number of records that is larger than can
        // actually fit in the node. Clamp it to a maximum of 64.
        var numberOfRecords = Math.Min(64, Descriptor.RecordCount + 1);

        // 3.1.3. The B-tree record offsets
        // The B-tree record offsets are an array of 16-bit integers relative from the
        // start of the B-tree node record. The first record offset is found at node
        // size - 2, e.g. 512 - 2 = 510, the second 2 bytes before that, e.g. 508, etc.
        // An additional record offset is added at the end to signify the start of the
        // free space.
        RecordOffsets = new BTRecordOffset[numberOfRecords];

        // Iterate in reverse to calculate offsets and sizes in a single pass.
        // By going backwards, we already have the "next" offset from the previous iteration.
        int nextOffset = 0;
        for (int i = numberOfRecords - 1; i >= 0; i--)
        {
            int entryOffset = 512 - 2 - (i * 2);
            int currentOffset = BinaryPrimitives.ReadUInt16BigEndian(data[entryOffset..]);
            
            RecordOffsets[i] = new BTRecordOffset
            {
                Offset = currentOffset,
                Size = nextOffset - currentOffset
            };
            
            nextOffset = currentOffset;
        }
    }
}
