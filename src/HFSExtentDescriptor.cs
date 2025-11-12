using System.Buffers.Binary;
using System.Diagnostics;

namespace HfsReader;

/// <summary>
/// Represents an HFS extent descriptor, which describes a contiguous range of allocation blocks.
/// </summary>
public struct HFSExtentDescriptor
{
    /// <summary>
    /// Gets the starting block of the extent.
    /// </summary>
    public ushort StartBlock { get; }

    /// <summary>
    /// Gets the number of blocks in the extent.
    /// </summary>
    public ushort BlockCount { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HFSExtentDescriptor"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the extent descriptor data.</param>
    public HFSExtentDescriptor(Span<byte> data)
    {
        if (data.Length != 4)
        {
            throw new ArgumentException("HFS Extent Descriptor data must be exactly 4 bytes long.", nameof(data));
        }

        int offset = 0;

        // Starting block of the extent.
        StartBlock = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        // Number of blocks in the extent.
        BlockCount = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        Debug.Assert(offset == data.Length);
    }
}
