using System.Diagnostics;
using HfsReader.Utilities;

namespace HfsReader;

public struct HFSFolderInformation
{
    public const int Size = 16;

    public ushort WindowTop { get; }
    
    public ushort WindowLeft { get; }
    
    public ushort WindowBottom { get; }
    
    public ushort WindowRight { get; }

    public HFSFinderFlags FinderFlags { get; }

    public ushort ParentLocationX { get; }

    public ushort ParentLocationY { get; }

    public ushort FolderView { get; }

    public HFSFolderInformation(Span<byte> data)
    {
        if (data.Length < Size)
        {
            throw new ArgumentException($"Folder information data must be at least {Size} bytes long.", nameof(data));
        }

        int offset = 0;

        // Window boundaries
        // The position and dimension of the folder’s window
        // Contains top, left, bottom, right-coordinate values
        WindowTop = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        WindowLeft = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        WindowBottom = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        WindowRight = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // Finder flags
        // See section: Finder flags
        FinderFlags = (HFSFinderFlags)SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // Location within the parent
        // Contains x and y-coordinate values
        // If set to {0, 0}, the Finder will place the item automatically
        ParentLocationX = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        ParentLocationY = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // Folder view
        // The manner in which folders are displayed.
        FolderView = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        Debug.Assert(offset == Size);
    }
}
