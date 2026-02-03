using System.Buffers.Binary;
using System.Diagnostics;
using HfsReader.Utilities;

namespace HfsReader;

/// <summary>
/// Represents a catalog key in the HFS catalog B-tree.
/// </summary>
public readonly struct HfsCatalogKey : IBTKey<HfsCatalogKey, HfsCatalogKeyComparison>
{
    /// <summary>
    /// Gets the size of the key data.
    /// </summary>
    public sbyte KeySize { get; }

    /// <summary>
    /// Gets the reserved byte in the key.
    /// </summary>
    public byte Reserved { get; }

    /// <summary>
    /// Gets the parent identifier (CNID) for this key.
    /// </summary>
    public uint ParentIdentifier { get; }

    /// <summary>
    /// Gets the name associated with this key.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HfsCatalogKey"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the key data.</param>
    /// <param name="bytesRead">Outputs the number of bytes read from the data span.</param>
    /// <exception cref="ArgumentException">Thrown when the data length is insufficient.</exception>
    public HfsCatalogKey(ReadOnlySpan<byte> data, out int bytesRead)
    {
        if (data.Length < 1)
        {
            throw new ArgumentException("Catalog index key data must be at least 1 byte long.", nameof(data));
        }

        int offset = 0;

        // The key data size
        // Signed 8-bit integer
        // Contains number of bytes
        KeySize = (sbyte)data[offset];
        offset += 1;

        if (KeySize > 0)
        {
            if (KeySize < 6 || KeySize > 37)
            {
                throw new InvalidDataException($"Invalid HfsCatalogKey size of {KeySize}.");
            }
            if (KeySize > data.Length)
            {
                throw new InvalidDataException($"HfsCatalogKey size of {KeySize} exceeds available data length of {data.Length - 1}.");
            }

            // Unknown (Reserved)
            Reserved = data[offset];
            offset += 1;

            // The parent identifier
            // Contains a CNID
            ParentIdentifier = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
            offset += 4;

            // Number of characters in the name string
            // The end-of-string character is not included
            // Name string
            // Contains an ASCII string with end-of-string character
            // Contains the name of the file or directory
            Name = SpanUtilities.ReadPascalString(data.Slice(offset, 32));
        }

        bytesRead = 1 + KeySize;
        Debug.Assert(bytesRead <= data.Length, "Did not read more bytes than available in HfsCatalogKey.");
    }

    /// <inheritdoc/>
    public static HfsCatalogKey Create(ReadOnlySpan<byte> data, out int bytesRead) => new(data, out bytesRead);

    /// <inheritdoc/>
    public int CompareTo(HfsCatalogKeyComparison other)
    {
        int parentComparison = ParentIdentifier.CompareTo(other.ParentIdentifier);
        if (parentComparison != 0)
        {
            return parentComparison;
        }

        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public bool IsParent(HfsCatalogKeyComparison other)
    {
        if (string.IsNullOrEmpty(other.Name) && ParentIdentifier == other.ParentIdentifier)
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public override string ToString()
        => $"HfsCatalogKey(ParentIdentifier={ParentIdentifier}, Name='{Name}')";
}
