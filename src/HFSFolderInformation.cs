using System.Buffers.Binary;
using System.Diagnostics;

namespace HfsReader;

/// <summary>
/// Represents folder information for an HFS folder.
/// </summary>
public struct HFSFolderInformation
{
    /// <summary>
    /// The size of the folder information structure in bytes.
    /// </summary>
    public const int Size = 16;

    /// <summary>Gets the top coordinate of the folder's window.</summary>
    public ushort WindowTop { get; }
    
    /// <summary>Gets the left coordinate of the folder's window.</summary>
    public ushort WindowLeft { get; }
    
    /// <summary>Gets the bottom coordinate of the folder's window.</summary>
    public ushort WindowBottom { get; }
    
    /// <summary>Gets the right coordinate of the folder's window.</summary>
    public ushort WindowRight { get; }

    /// <summary>Gets the Finder flags.</summary>
    public HFSFinderFlags FinderFlags { get; }

    /// <summary>Gets the X-coordinate of the folder's location within the parent.</summary>
    public ushort ParentLocationX { get; }

    /// <summary>Gets the Y-coordinate of the folder's location within the parent.</summary>
    public ushort ParentLocationY { get; }

    /// <summary>Gets the folder view (manner in which folders are displayed).</summary>
    public ushort FolderView { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HFSFolderInformation"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the folder information data.</param>
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
