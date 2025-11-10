using System.Diagnostics;
using HfsReader.Utilities;

namespace HfsReader;

public struct HFSExtendedFileInformation
{
    public const int Size = 16;

    public ushort IconIdentifier { get; }

    public ushort Reserved1 { get; }

    public ushort Reserved2 { get; }

    public ushort Reserved3 { get; }

    public byte ExtendedFinderScriptCodeFlags { get; }

    public byte ExtendedFinderFlags { get; }

    public short Comment { get; }

    public uint PutAwayFolderIdentifier { get; }

    public HFSExtendedFileInformation(Span<byte> data)
    {
        if (data.Length < Size)
        {
            throw new ArgumentException($"Extended file information data must be at least {Size} bytes long.", nameof(data));
        }

        int offset = 0;

        // Icon identifier
        // An identifier, assigned by the Finder, of the file’s icon.
        IconIdentifier = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // Unknown (Reserved)
        // Array of signed 16-bit integers
        Reserved1 = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        Reserved2 = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        Reserved3 = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // Extended finder script code flags
        // These flags are used if the script code flag is set.
        ExtendedFinderScriptCodeFlags = data[offset];
        offset += 1;

        // Extended finder flags
        // See section: Extended finder flags
        ExtendedFinderFlags = data[offset];
        offset += 1;

        // Comment
        // Signed 16-bit integer
        // If the high-bit is clear, an identifier, assigned by the Finder, for the
        // comment that is displayed in the information window when the user selects
        // a file and chooses the Get Info command from the File menu.
        Comment = SpanUtilities.ReadInt16BE(data, offset);
        offset += 2;

        // Put away folder identifier
        // Contains a CNID
        PutAwayFolderIdentifier = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        Debug.Assert(offset == Size);
    }
}