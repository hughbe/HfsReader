using System.Buffers.Binary;
using HfsReader.Utilities;

namespace HfsReader;

/// <summary>
/// Represents a catalog index key in the HFS catalog B-tree.
/// </summary>
public struct HFSCatalogIndexKey
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
    /// Initializes a new instance of the <see cref="HFSCatalogIndexKey"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the key data.</param>
    public HFSCatalogIndexKey(Span<byte> data)
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
                throw new InvalidDataException($"Invalid HFSCatalogIndexKey size of {KeySize}.");
            }
            if (KeySize > data.Length)
            {
                throw new InvalidDataException($"HFSCatalogIndexKey size of {KeySize} exceeds available data length of {data.Length - 1}.");
            }

            // Unknown (Reserved)
            Reserved = data[offset];
            offset += 1;

            // The parent identifier
            // Contains a CNID
            ParentIdentifier = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
            offset += 4;

            // Number of characters in the name string
            // The end-of-string character is not included
            // Name string
            // Contains an ASCII string with end-of-string character
            // Contains the name of the file or directory
            Name = SpanUtilities.ReadFixedLengthStringWithLength(data, offset, 32);
        }
    }

    /// <summary>
    /// Compares this key to the specified parent identifier and name.
    /// </summary>
    /// <param name="parentID">The parent identifier to compare.</param>
    /// <param name="name">The name to compare.</param>
    /// <returns>An integer indicating the relative order.</returns>
    public int CompareTo(uint parentID, string name)
    {
        int parentComparison = ParentIdentifier.CompareTo(parentID);
        if (parentComparison != 0)
        {
            return parentComparison;
        }

        return string.Compare(Name, name, StringComparison.Ordinal);
    }
}
