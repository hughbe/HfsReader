using System.Buffers.Binary;
using System.Text;

namespace HfsReader.Utilities;

internal static class SpanUtilities
{
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
        var hfsTimestamp = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));

        // HFS timestamps are seconds since 00:00:00 on January 1, 1904
        var hfsEpoch = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return hfsEpoch.AddSeconds(hfsTimestamp);
    }
}
