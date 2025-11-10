using System.Diagnostics;
using HfsReader.Utilities;

namespace HfsReader;

public struct HFSExtendedFolderInformation
{
    public const int Size = 16;

    public ushort ScrollPositionX { get; }

    public ushort ScrollPositionY { get; }

    public uint OpenDirectoryIdentifierChainOrDateAdded { get; }

    public byte ExtendedFinderScriptCodeFlags { get; }

    public byte ExtendedFinderFlags { get; }
    
    public short Comment { get; }

    public uint PutAwayFolderIdentifier { get; }

    public HFSExtendedFolderInformation(Span<byte> data)
    {
        if (data.Length < Size)
        {
            throw new ArgumentException($"Extended folder information data must be at least {Size} bytes long.", nameof(data));
        }

        int offset = 0;

        // Scroll position
        // The scroll position for icon views
        // Contains x and y-coordinate values
        ScrollPositionX = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        ScrollPositionY = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // If kHFSHasDateAddedMask is not set
        // Open directory identifier chain
        // Signed 32-bit integer
        // Chain of directory identifiers for open folders.
        // Added date and time
        // Contains a POSIX timestamp in UTC
        OpenDirectoryIdentifierChainOrDateAdded = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        // Extended finder script code flags
        // These flags are used if the script code flag is set.
        ExtendedFinderScriptCodeFlags = data[offset];
        offset += 1;

        // Extended Finder flags
        // See section: Extended finder flags
        ExtendedFinderFlags = data[offset];
        offset += 1;

        // Comment
        // Signed 16-bit integer
        // If the high-bit is clear, an identifier, assigned by the Finder, for the
        // comment that is displayed in the information window when the user selects
        // a folder and chooses the Get Info command from the File menu.
        Comment = SpanUtilities.ReadInt16BE(data, offset);
        offset += 2;

        // Put away folder identifier
        // Contains a CNID
        PutAwayFolderIdentifier = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        Debug.Assert(offset == Size);
    }
}
