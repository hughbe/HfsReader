using System.Buffers.Binary;
using System.Diagnostics;

namespace HfsReader;

/// <summary>
/// Represents extended folder information for an HFS folder.
/// </summary>
public struct HFSExtendedFolderInformation
{
    /// <summary>
    /// The size of the extended folder information structure in bytes.
    /// </summary>
    public const int Size = 16;

    /// <summary>Gets the X-coordinate of the scroll position for icon views.</summary>
    public ushort ScrollPositionX { get; }

    /// <summary>Gets the Y-coordinate of the scroll position for icon views.</summary>
    public ushort ScrollPositionY { get; }

    /// <summary>Gets the open directory identifier chain or date added.</summary>
    public uint OpenDirectoryIdentifierChainOrDateAdded { get; }

    /// <summary>Gets the extended Finder script code flags.</summary>
    public byte ExtendedFinderScriptCodeFlags { get; }

    /// <summary>Gets the extended Finder flags.</summary>
    public byte ExtendedFinderFlags { get; }
    
    /// <summary>Gets the comment identifier assigned by the Finder.</summary>
    public short Comment { get; }

    /// <summary>Gets the put-away folder identifier (CNID).</summary>
    public uint PutAwayFolderIdentifier { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HFSExtendedFolderInformation"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the extended folder information data.</param>
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
        ScrollPositionX = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        ScrollPositionY = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // If kHFSHasDateAddedMask is not set
        // Open directory identifier chain
        // Signed 32-bit integer
        // Chain of directory identifiers for open folders.
        // Added date and time
        // Contains a POSIX timestamp in UTC
        OpenDirectoryIdentifierChainOrDateAdded = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
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
        Comment = BinaryPrimitives.ReadInt16BigEndian(data[offset..]);
        offset += 2;

        // Put away folder identifier
        // Contains a CNID
        PutAwayFolderIdentifier = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        Debug.Assert(offset == Size);
    }
}
