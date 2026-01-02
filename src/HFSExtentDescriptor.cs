using System.Buffers.Binary;
using System.Diagnostics;

namespace HfsReader;

/// <summary>
/// Represents an HFS extent descriptor, which describes a contiguous range of allocation blocks.
/// </summary>
public readonly struct HFSExtentDescriptor
{
    /// <summary>
    /// The size of an HFS extent descriptor in bytes.
    /// </summary>
    public const int Size = 4;
    
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
    /// <exception cref="ArgumentException">Thrown when the data length is not equal to the required size.</exception>
    public HFSExtentDescriptor(Span<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data length must be exactly {Size} bytes to read an HFSExtentDescriptor.", nameof(data));
        }

        int offset = 0;

        // Starting block of the extent.
        StartBlock = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // Number of blocks in the extent.
        BlockCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        Debug.Assert(offset == data.Length, "Did not read exactly the expected number of bytes for HFSExtentDescriptor.");
    }
}
