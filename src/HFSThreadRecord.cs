using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

namespace HfsReader;

/// <summary>
/// Represents an HFS thread record (file or folder thread) in the catalog.
/// </summary>
public struct HFSThreadRecord
{
    /// <summary>
    /// The minimum size of a thread record in bytes.
    /// </summary>
    public const int MinSize = 15;
    
    /// <summary>
    /// Gets the catalog data record type.
    /// </summary>
    public HFSCatalogDataRecordType Type { get; }
    
    /// <summary>
    /// Gets the first reserved field.
    /// </summary>
    public uint Reserved1 { get; }
    
    /// <summary>
    /// Gets the second reserved field.
    /// </summary>
    public uint Reserved2 { get; }

    /// <summary>
    /// Gets the parent identifier (CNID).
    /// </summary>
    public uint ParentIdentifier { get; }

    /// <summary>
    /// Gets the length of the name string.
    /// </summary>
    public byte NameLength { get; }

    /// <summary>
    /// Gets the name of the associated file or directory.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HFSThreadRecord"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the thread record data.</param>
    public HFSThreadRecord(Span<byte> data)
    {
        if (data.Length < MinSize)
        {
            throw new ArgumentException($"Thread record data must be at least {MinSize} bytes long.", nameof(data));
        }

        int offset = 0;

        // The record type
        Type = (HFSCatalogDataRecordType)BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;
        if (Type != HFSCatalogDataRecordType.FileThread && Type != HFSCatalogDataRecordType.FolderThread)
        {
            throw new ArgumentException($"Expected a thread record type, but found {Type}.", nameof(data));
        }

        // Unknown (Reserved)
        // Array of 32-bit integer values
        Reserved1 = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        Reserved2 = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // The parent identifier
        // Contains a CNID
        ParentIdentifier = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Number of characters in the name string
        NameLength = data[offset];
        offset += 1;

        // Name string ASCII string
        // Contains the name of the associated file or directory
        Name = Encoding.ASCII.GetString(data.Slice(offset, NameLength));
        offset += NameLength;

        Debug.Assert(offset == MinSize + NameLength);
    }
}