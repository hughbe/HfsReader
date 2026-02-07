using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HfsReader;

/// <summary>
/// Represents an HFS timestamp, stored as seconds since the MacOS epoch (January 1, 1904 00:00:00 UTC).
/// </summary>
public readonly struct HfsTimestamp : IEquatable<HfsTimestamp>, IComparable<HfsTimestamp>
{
    /// <summary>
    /// The size of an HFS timestamp in bytes.
    /// </summary>
    public const int Size = 4;

    /// <summary>
    /// The MacOS epoch (January 1, 1904 00:00:00 UTC).
    /// </summary>
    private static readonly DateTime MacOSEpoch = new(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Gets the raw timestamp value (seconds since the MacOS epoch).
    /// </summary>
    public uint Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HfsTimestamp"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the big-endian 4-byte timestamp.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HfsTimestamp(ReadOnlySpan<byte> data)
    {
        Debug.Assert(data.Length >= Size, "Data span must contain at least 4 bytes for the timestamp.");

        Value = BinaryPrimitives.ReadUInt32BigEndian(data);
    }

    /// <summary>
    /// Converts the HFS timestamp to a <see cref="DateTime"/>.
    /// </summary>
    /// <returns>The corresponding <see cref="DateTime"/> value in UTC.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTime ToDateTime() => MacOSEpoch.AddSeconds(Value);

    /// <summary>
    /// Converts the <see cref="HfsTimestamp"/> to a <see cref="DateTime"/>.
    /// </summary>
    /// <param name="timestamp">The <see cref="HfsTimestamp"/> instance.</param>
    /// <returns>The corresponding <see cref="DateTime"/> value.</returns>
    public static implicit operator DateTime(HfsTimestamp timestamp) => timestamp.ToDateTime();

    /// <inheritdoc/>
    public bool Equals(HfsTimestamp other) => Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is HfsTimestamp other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    /// <inheritdoc/>
    public int CompareTo(HfsTimestamp other) => Value.CompareTo(other.Value);

    /// <summary>
    /// Determines whether two <see cref="HfsTimestamp"/> instances are equal.
    /// </summary>
    public static bool operator ==(HfsTimestamp left, HfsTimestamp right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="HfsTimestamp"/> instances are not equal.
    /// </summary>
    public static bool operator !=(HfsTimestamp left, HfsTimestamp right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => ToDateTime().ToString("O");
}
