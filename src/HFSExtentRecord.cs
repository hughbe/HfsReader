using System.Diagnostics;

namespace HfsReader;

public struct HFSExtentRecord
{
    public const int Size = 12;
    
    private HFSExtentDescriptor FirstExtent { get; }
    
    private HFSExtentDescriptor SecondExtent { get; }
    
    private HFSExtentDescriptor ThirdExtent { get; }

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