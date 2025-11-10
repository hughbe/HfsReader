using System.Text;

namespace HfsReader.Utilities;

internal static class SpanUtilities
{
    public static short ReadInt16BE(ReadOnlySpan<byte> data, int offset)
    {
        return (short)((data[offset] << 8) | data[offset + 1]);
    }

    public static ushort ReadUInt16BE(ReadOnlySpan<byte> data, int offset)
    {
        return (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    public static uint ReadUInt32BE(ReadOnlySpan<byte> data, int offset)
    {
        return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
    }

    public static ulong ReadUInt64BE(ReadOnlySpan<byte> data, int offset)
    {
        return ((ulong)ReadUInt32BE(data, offset) << 32) | ReadUInt32BE(data, offset + 4);
    }

    public static string ReadFixedLengthString(ReadOnlySpan<byte> data, int offset, int length)
    {
        // Read the string bytes for the fixed length up to the first null terminator.
        var stringData = data.Slice(offset, length);
        return stringData.IndexOf((byte)'\0') is int nullIndex
            ? Encoding.ASCII.GetString(stringData[..nullIndex])
            : Encoding.ASCII.GetString(stringData);
    }

    public static string ReadFixedLengthStringWithLength(ReadOnlySpan<byte> data, int offset, int length)
    {
        // There is one extra byte for the length.
        var actualLength = data[offset];
        offset += 1;

        // Read the string bytes for the fixed length.
        return Encoding.ASCII.GetString(data.Slice(offset, Math.Min(actualLength, length)));
    }

    public static string ReadString(ReadOnlySpan<byte> data, int offset, int length)
    {
        // Read the string bytes for the fixed length.
        return Encoding.ASCII.GetString(data.Slice(offset, length));
    }

    public static DateTime ReadHfsTimestamp(ReadOnlySpan<byte> data, int offset)
    {
        // 4 bytes HFS timestamp
        var hfsTimestamp = ReadUInt32BE(data, offset);

        // HFS timestamps are seconds since 00:00:00 on January 1, 1904
        var hfsEpoch = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return hfsEpoch.AddSeconds(hfsTimestamp);
    }    
}
