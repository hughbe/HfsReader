using System.Buffers.Binary;
using System.Diagnostics;

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
        WindowTop = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        WindowLeft = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        WindowBottom = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        WindowRight = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        // Finder flags
        // See section: Finder flags
        FinderFlags = (HFSFinderFlags)BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        // Location within the parent
        // Contains x and y-coordinate values
        // If set to {0, 0}, the Finder will place the item automatically
        ParentLocationX = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        ParentLocationY = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        // Folder view
        // The manner in which folders are displayed.
        FolderView = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        Debug.Assert(offset == Size);
    }
}
