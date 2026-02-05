using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace HfsReader.Utilities;

/// <summary>
/// Represents a fixed-size Pascal string of up to 16 bytes (15 chars + 1 length byte).
/// </summary>
[InlineArray(Size)]
public struct String16 : ISpanFormattable, IEquatable<String16>
{
    /// <summary>
    /// Gets the size of the string in bytes.
    /// </summary>
    public const int Size = 16;

    /// <summary>
    /// The first element of the array.
    /// </summary>
    private byte _element0;

    /// <summary>
    /// Initializes a new instance of the <see cref="String16"/> struct.
    /// </summary>
    /// <param name="data">The span containing the string bytes.</param>
    /// <exception cref="ArgumentException">Thrown when the data span length is greater than <see cref="Size"/>.</exception>
    public String16(ReadOnlySpan<byte> data)
    {
        if (data.Length > Size)
        {
            throw new ArgumentException($"Data span must be at most {Size} bytes long.", nameof(data));
        }

        data.CopyTo(AsSpan());
    }

    /// <summary>
    /// Gets the length of the string content (first byte is Pascal length prefix).
    /// </summary>
    public readonly int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> span = AsReadOnlySpan();
            // Pascal string: first byte is the length
            int declaredLength = span[0];
            // Clamp to available space (Size - 1 for the length byte)
            return Math.Min(declaredLength, Size - 1);
        }
    }

    /// <summary>
    /// Gets a span over the elements of the array.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan() =>
        MemoryMarshal.CreateSpan(ref _element0, Size);

    /// <summary>
    /// Gets a read-only span over the elements of the array.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<byte> AsReadOnlySpan() =>
        MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _element0), Size);

    /// <summary>
    /// Gets a read-only span over just the string content (excluding the length byte).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<byte> AsContentSpan() =>
        AsReadOnlySpan().Slice(1, Length);

    /// <summary>
    /// Attempts to format the string into the provided span without allocating.
    /// </summary>
    /// <param name="destination">The span to write the string to.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <returns>true if the formatting was successful; otherwise, false.</returns>
    public readonly bool TryFormat(Span<char> destination, out int charsWritten)
    {
        int length = Length;

        if (destination.Length < length)
        {
            charsWritten = 0;
            return false;
        }

        ReadOnlySpan<byte> content = AsContentSpan();
        for (int i = 0; i < length; i++)
        {
            destination[i] = (char)content[i];
        }

        charsWritten = length;
        return true;
    }

    /// <inheritdoc/>
    readonly bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        TryFormat(destination, out charsWritten);

    /// <inheritdoc/>
    readonly string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <inheritdoc/>
    public override readonly string ToString()
    {
        ReadOnlySpan<byte> content = AsContentSpan();
        return Encoding.ASCII.GetString(content);
    }

    /// <summary>
    /// Determines whether this string equals the specified character span without allocating.
    /// </summary>
    /// <param name="other">The character span to compare with.</param>
    /// <returns>true if the strings are equal; otherwise, false.</returns>
    public readonly bool Equals(ReadOnlySpan<char> other)
    {
        int length = Length;
        if (other.Length != length)
        {
            return false;
        }

        ReadOnlySpan<byte> content = AsContentSpan();
        for (int i = 0; i < length; i++)
        {
            if ((char)content[i] != other[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether this string equals the specified byte span (ASCII) without allocating.
    /// </summary>
    /// <param name="other">The byte span to compare with.</param>
    /// <returns>true if the strings are equal; otherwise, false.</returns>
    public readonly bool Equals(ReadOnlySpan<byte> other)
    {
        int length = Length;
        if (other.Length != length)
        {
            return false;
        }

        return AsContentSpan().SequenceEqual(other);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(String16 other) =>
        AsReadOnlySpan().SequenceEqual(other.AsReadOnlySpan());

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj) =>
        obj is String16 other && Equals(other);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        hash.AddBytes(AsContentSpan());
        return hash.ToHashCode();
    }

    /// <summary>
    /// Determines whether two <see cref="String16"/> instances are equal.
    /// </summary>
    public static bool operator ==(String16 left, String16 right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="String16"/> instances are not equal.
    /// </summary>
    public static bool operator !=(String16 left, String16 right) => !left.Equals(right);

    /// <summary>
    /// Implicitly converts the <see cref="String16"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="str">The <see cref="String16"/> instance.</param>
    /// <returns>The converted string.</returns>
    public static implicit operator string(String16 str) => str.ToString();
}
