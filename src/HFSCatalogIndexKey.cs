using System.Buffers.Binary;
using HfsReader.Utilities;

namespace HfsReader;

public struct HFSCatalogIndexKey
{
    public sbyte KeySize { get; }
    public byte Reserved { get; }
    public uint ParentIdentifier { get; }
    public string? Name { get; }

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
