using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

namespace HfsReader;

public struct HFSThreadRecord
{
    public const int MinSize = 15;
    
    public HFSCatalogDataRecordType Type { get; }
    
    public uint Reserved1 { get; }
    
    public uint Reserved2 { get; }

    public uint ParentIdentifier { get; }

    public byte NameLength { get; }

    public string Name { get; }

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