using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace HfsReader.Utilities;

/// <summary>
/// Represents a fixed-size string of up to 31 bytes (e.g., volume names).
/// </summary>
[InlineArray(Size)]
public struct String31 : ISpanFormattable, IEquatable<String31>, IComparable<String31>
{
    /// <summary>
    /// Gets the size of the string in bytes.
    /// </summary>
    public const int Size = 31;

    /// <summary>
    /// The first element of the array.
    /// </summary>
    private byte _element0;

    /// <summary>
    /// Initializes a new instance of the <see cref="String31"/> struct.
    /// </summary>
    /// <param name="data">The span containing the string bytes.</param>
    /// <exception cref="ArgumentException">Thrown when the data span length is greater than <see cref="Size"/>.</exception>
    public String31(ReadOnlySpan<byte> data)
    {
        if (data.Length > Size)
        {
            throw new ArgumentException($"Data span must be at most {Size} bytes long.", nameof(data));
        }

        data.CopyTo(AsSpan());
    }

    /// <summary>
    /// Gets the length of the string (excluding null terminator).
    /// </summary>
    public readonly int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> span = AsReadOnlySpan();
            int length = span.IndexOf((byte)0);
            return length < 0 ? span.Length : length;
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
    /// Attempts to format the string into the provided span without allocating.
    /// </summary>
    /// <param name="destination">The span to write the string to.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <returns>true if the formatting was successful; otherwise, false.</returns>
    public readonly bool TryFormat(Span<char> destination, out int charsWritten)
    {
        ReadOnlySpan<byte> span = AsReadOnlySpan();
        int length = Length;

        if (destination.Length < length)
        {
            charsWritten = 0;
            return false;
        }

        for (int i = 0; i < length; i++)
        {
            destination[i] = (char)span[i];
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
        ReadOnlySpan<byte> span = AsReadOnlySpan();
        int length = Length;

        return Encoding.ASCII.GetString(span[..length]);
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

        ReadOnlySpan<byte> span = AsReadOnlySpan();
        for (int i = 0; i < length; i++)
        {
            if ((char)span[i] != other[i])
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

        return AsReadOnlySpan()[..length].SequenceEqual(other);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(String31 other) =>
        AsReadOnlySpan().SequenceEqual(other.AsReadOnlySpan());

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj) =>
        obj is String31 other && Equals(other);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        ReadOnlySpan<byte> span = AsReadOnlySpan();
        var hash = new HashCode();
        hash.AddBytes(span[..Length]);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Compares this string to another <see cref="String31"/> using ordinal comparison without allocating.
    /// </summary>
    /// <param name="other">The other string to compare with.</param>
    /// <returns>A value indicating the relative order of the strings.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int CompareTo(String31 other)
    {
        ReadOnlySpan<byte> thisSpan = AsReadOnlySpan()[..Length];
        ReadOnlySpan<byte> otherSpan = other.AsReadOnlySpan()[..other.Length];
        return thisSpan.SequenceCompareTo(otherSpan);
    }

    /// <summary>
    /// Compares this string to a character span using ordinal comparison without allocating.
    /// </summary>
    /// <param name="other">The character span to compare with.</param>
    /// <returns>A value indicating the relative order of the strings.</returns>
    public readonly int CompareTo(ReadOnlySpan<char> other)
    {
        ReadOnlySpan<byte> span = AsReadOnlySpan();
        int thisLength = Length;
        int otherLength = other.Length;
        int minLength = Math.Min(thisLength, otherLength);

        for (int i = 0; i < minLength; i++)
        {
            int cmp = span[i].CompareTo((byte)other[i]);
            if (cmp != 0)
            {
                return cmp;
            }
        }

        return thisLength.CompareTo(otherLength);
    }

    /// <summary>
    /// Determines whether two <see cref="String31"/> instances are equal.
    /// </summary>
    public static bool operator ==(String31 left, String31 right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="String31"/> instances are not equal.
    /// </summary>
    public static bool operator !=(String31 left, String31 right) => !left.Equals(right);

    /// <summary>
    /// Implicitly converts the <see cref="String31"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="str">The <see cref="String31"/> instance.</param>
    /// <returns>The converted string.</returns>
    public static implicit operator string(String31 str) => str.ToString();
}
