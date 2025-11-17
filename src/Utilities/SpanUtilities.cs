using System.Buffers.Binary;
using System.Text;

namespace HfsReader.Utilities;

/// <summary>
/// Provides utility methods for reading data from spans.
/// </summary>
internal static class SpanUtilities
{
    /// <summary>
    /// Reads a fixed-length ASCII string from the specified span, stopping at the first null terminator.
    /// </summary>
    /// <param name="data">The span containing the data.</param>
    /// <param name="offset">The offset at which the string starts.</param>
    /// <param name="length">The length of the string to read.</param>
    /// <returns>The decoded string.</returns>
    public static string ReadFixedLengthString(ReadOnlySpan<byte> data, int offset, int length)
    {
        // Read the string bytes for the fixed length up to the first null terminator.
        var stringData = data.Slice(offset, length);
        return stringData.IndexOf((byte)'\0') is int nullIndex
            ? Encoding.ASCII.GetString(stringData[..nullIndex])
            : Encoding.ASCII.GetString(stringData);
    }

    /// <summary>
    /// Reads a fixed-length ASCII string with a length prefix from the specified span.
    /// </summary>
    /// <param name="data">The span containing the data.</param>
    /// <param name="offset">The offset at which the length-prefixed string starts.</param>
    /// <param name="length">The maximum length of the string to read.</param>
    /// <returns>The decoded string.</returns>
    public static string ReadFixedLengthStringWithLength(ReadOnlySpan<byte> data, int offset, int length)
    {
        // There is one extra byte for the length.
        var actualLength = data[offset];
        offset += 1;

        // Read the string bytes for the fixed length.
        return Encoding.ASCII.GetString(data.Slice(offset, Math.Min(actualLength, length)));
    }

    /// <summary>
    /// Reads an ASCII string of the specified length from the span.
    /// </summary>
    /// <param name="data">The span containing the data.</param>
    /// <param name="offset">The offset at which the string starts.</param>
    /// <param name="length">The length of the string to read.</param>
    /// <returns>The decoded string.</returns>
    public static string ReadString(ReadOnlySpan<byte> data, int offset, int length)
    {
        // Read the string bytes for the fixed length.
        return Encoding.ASCII.GetString(data.Slice(offset, length));
    }

    /// <summary>
    /// Reads an HFS timestamp from the specified span and converts it to a <see cref="DateTime"/>.
    /// </summary>
    /// <param name="data">The span containing the data.</param>
    /// <param name="offset">The offset at which the timestamp starts.</param>
    /// <returns>The corresponding <see cref="DateTime"/> value.</returns>
    public static DateTime ReadHfsTimestamp(ReadOnlySpan<byte> data, int offset)
    {
        // 4 bytes HFS timestamp
        var hfsTimestamp = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);

        // HFS timestamps are seconds since 00:00:00 on January 1, 1904
        var hfsEpoch = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return hfsEpoch.AddSeconds(hfsTimestamp);
    }
}
