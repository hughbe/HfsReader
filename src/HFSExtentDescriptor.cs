using System.Diagnostics;
using HfsReader.Utilities;

namespace HfsReader;

public struct HFSExtentDescriptor
{
    public ushort StartBlock { get; }

    public ushort BlockCount { get; }

    public HFSExtentDescriptor(Span<byte> data)
    {
        if (data.Length != 4)
        {
            throw new ArgumentException("HFS Extent Descriptor data must be exactly 4 bytes long.", nameof(data));
        }

        int offset = 0;

        // Starting block of the extent.
        StartBlock = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // Number of blocks in the extent.
        BlockCount = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        Debug.Assert(offset == data.Length);
    }
}
