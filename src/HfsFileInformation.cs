using System.Buffers.Binary;
using System.Diagnostics;

namespace HfsReader;

/// <summary>
/// Represents file information for an HFS file.
/// </summary>
public struct HfsFileInformation
{
    /// <summary>
    /// The size of the file information structure in bytes.
    /// </summary>
    public const int Size = 16;

    /// <summary>
    /// Gets the file type.
    /// </summary>
    public uint FileType { get; }

    /// <summary>
    /// Gets the file creator.
    /// </summary>
    public uint Creator { get; }

    /// <summary>
    /// Gets the Finder flags.
    /// </summary>
    public HfsFinderFlags FinderFlags { get; }

    /// <summary>
    /// Gets the X-coordinate of the file's location within the parent.
    /// </summary>
    public ushort ParentLocationX { get; }

    /// <summary>
    /// Gets the Y-coordinate of the file's location within the parent.
    /// </summary>
    public ushort ParentLocationY { get; }

    /// <summary>
    /// Gets the window in which the file's icon appears.
    /// </summary>
    public ushort FileIconWindow { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HfsFileInformation"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the file information data.</param>
    /// <exception cref="ArgumentException">Thrown when the data length is not equal to the expected size.</exception>
    public HfsFileInformation(Span<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"File information data must be exactly {Size} bytes long.", nameof(data));
        }

        int offset = 0;

        // File type
        // Array of unsigned 8-bit integers
        FileType = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Creator
        // Array of unsigned 8-bit integers
        Creator = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Finder flags
        // See section: Finder flags
        FinderFlags = (HfsFinderFlags)BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // Location within the parent
        // Contains x and y-coordinate values
        // If set to {0, 0}, the Finder will place the item automatically
        ParentLocationX = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        ParentLocationY = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // File icon window
        // The window in which the file's icon appears.
        FileIconWindow = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        Debug.Assert(offset == data.Length, "Did not read the expected number of bytes for HfsFileInformation.");
    }
}
