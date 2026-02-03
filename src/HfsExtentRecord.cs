using System.Diagnostics;

namespace HfsReader;

/// <summary>
/// Represents an HFS extent record, which contains up to three extent descriptors.
/// </summary>
public struct HfsExtentRecord
{
    /// <summary>
    /// The size of an HFS extent record in bytes.
    /// </summary>
    public const int Size = 12;
    
    /// <summary>
    /// Gets the first extent descriptor.
    /// </summary>
    private HfsExtentDescriptor FirstExtent { get; }
    
    /// <summary>
    /// Gets the second extent descriptor.
    /// </summary>
    private HfsExtentDescriptor SecondExtent { get; }
    
    /// <summary>
    /// Gets the third extent descriptor.
    /// </summary>
    private HfsExtentDescriptor ThirdExtent { get; }

    /// <summary>
    /// Gets the total number of blocks in the extents.
    /// </summary>
    public int TotalBlockCount => FirstExtent.BlockCount + SecondExtent.BlockCount + ThirdExtent.BlockCount;

    /// <summary>
    /// Gets the extent descriptor at the specified index (0, 1, or 2).
    /// </summary>
    /// <param name="index">The index of the extent descriptor (0, 1, or 2).</param>
    /// <returns>The <see cref="HfsExtentDescriptor"/> at the specified index.</returns>
    public HfsExtentDescriptor this[int index]
    {
        get
        {
            return index switch
            {
                0 => FirstExtent,
                1 => SecondExtent,
                2 => ThirdExtent,
                _ => throw new IndexOutOfRangeException("Hfs Extent Record only contains three extents (0, 1, 2)."),
            };
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HfsExtentRecord"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the extent record data.</param>
    /// <exception cref="ArgumentException">Thrown when the data length is not equal to the required size.</exception>
    public HfsExtentRecord(Span<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Hfs Extent Record data must be exactly {Size} bytes long.", nameof(data));
        }

        int offset = 0;

        // First extent descriptor.
        FirstExtent = new HfsExtentDescriptor(data.Slice(offset, 4));
        offset += 4;

        // Second extent descriptor.
        SecondExtent = new HfsExtentDescriptor(data.Slice(offset, 4));
        offset += 4;

        // Third extent descriptor.
        ThirdExtent = new HfsExtentDescriptor(data.Slice(offset, 4));
        offset += 4;

        Debug.Assert(offset == data.Length, "Did not read exactly the expected number of bytes for HfsExtentRecord.");
    }
}