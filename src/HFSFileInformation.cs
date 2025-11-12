using System.Buffers.Binary;
using System.Diagnostics;

namespace HfsReader;

public struct HFSFileInformation
{
    public const int Size = 16;

    public uint FileType { get; }

    public uint Creator { get; }

    public HFSFinderFlags FinderFlags { get; }

    public ushort ParentLocationX { get; }

    public ushort ParentLocationY { get; }

    public ushort FileIconWindow { get; }

    public HFSFileInformation(Span<byte> data)
    {
        if (data.Length < Size)
        {
            throw new ArgumentException($"File information data must be at least {Size} bytes long.", nameof(data));
        }

        int offset = 0;

        // File type
        // Array of unsigned 8-bit integers
        FileType = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Creator
        // Array of unsigned 8-bit integers
        Creator = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

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

        // File icon window
        // The window in which the file's icon appears.
        FileIconWindow = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        Debug.Assert(offset == Size);
    }
}
