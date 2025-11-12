using System.Diagnostics;

namespace HfsReader;

/// <summary>
/// Represents an HFS extent record, which contains up to three extent descriptors.
/// </summary>
public struct HFSExtentRecord
{
    /// <summary>
    /// The size of an HFS extent record in bytes.
    /// </summary>
    public const int Size = 12;
    
    private HFSExtentDescriptor FirstExtent { get; }
    
    private HFSExtentDescriptor SecondExtent { get; }
    
    private HFSExtentDescriptor ThirdExtent { get; }

    /// <summary>
    /// Gets the extent descriptor at the specified index (0, 1, or 2).
    /// </summary>
    /// <param name="index">The index of the extent descriptor (0, 1, or 2).</param>
    /// <returns>The <see cref="HFSExtentDescriptor"/> at the specified index.</returns>
    public HFSExtentDescriptor this[int index]
    {
        get
        {
            return index switch
            {
                0 => FirstExtent,
                1 => SecondExtent,
                2 => ThirdExtent,
                _ => throw new IndexOutOfRangeException("HFS Extent Record only contains three extents (0, 1, 2)."),
            };
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HFSExtentRecord"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the extent record data.</param>
    public HFSExtentRecord(Span<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"HFS Extent Record data must be exactly {Size} bytes long.", nameof(data));
        }

        int offset = 0;

        // First extent descriptor.
        FirstExtent = new HFSExtentDescriptor(data.Slice(offset, 4));
        offset += 4;

        // Second extent descriptor.
        SecondExtent = new HFSExtentDescriptor(data.Slice(offset, 4));
        offset += 4;

        // Third extent descriptor.
        ThirdExtent = new HFSExtentDescriptor(data.Slice(offset, 4));
        offset += 4;

        Debug.Assert(offset == data.Length);
    }
}