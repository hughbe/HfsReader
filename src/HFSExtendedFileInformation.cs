using System.Buffers.Binary;
using System.Diagnostics;

namespace HfsReader;

/// <summary>
/// Represents extended file information for an HFS file.
/// </summary>
public struct HFSExtendedFileInformation
{
    /// <summary>
    /// The size of the extended file information structure in bytes.
    /// </summary>
    public const int Size = 16;

    /// <summary>Gets the icon identifier assigned by the Finder.</summary>
    public ushort IconIdentifier { get; }

    /// <summary>Gets the first reserved field.</summary>
    public ushort Reserved1 { get; }

    /// <summary>Gets the second reserved field.</summary>
    public ushort Reserved2 { get; }

    /// <summary>Gets the third reserved field.</summary>
    public ushort Reserved3 { get; }

    /// <summary>Gets the extended Finder script code flags.</summary>
    public byte ExtendedFinderScriptCodeFlags { get; }

    /// <summary>Gets the extended Finder flags.</summary>
    public byte ExtendedFinderFlags { get; }

    /// <summary>Gets the comment identifier assigned by the Finder.</summary>
    public short Comment { get; }

    /// <summary>Gets the put-away folder identifier (CNID).</summary>
    public uint PutAwayFolderIdentifier { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HFSExtendedFileInformation"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the extended file information data.</param>
    public HFSExtendedFileInformation(Span<byte> data)
    {
        if (data.Length < Size)
        {
            throw new ArgumentException($"Extended file information data must be at least {Size} bytes long.", nameof(data));
        }

        int offset = 0;

        // Icon identifier
        // An identifier, assigned by the Finder, of the file's icon.
        IconIdentifier = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        // Unknown (Reserved)
        // Array of signed 16-bit integers
        Reserved1 = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        Reserved2 = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        Reserved3 = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
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
        Comment = BinaryPrimitives.ReadInt16BigEndian(data.Slice(offset));
        offset += 2;

        // Put away folder identifier
        // Contains a CNID
        PutAwayFolderIdentifier = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        Debug.Assert(offset == Size);
    }
}