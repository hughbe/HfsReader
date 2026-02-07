using System.Buffers.Binary;
using System.Diagnostics;
using System.Drawing;

namespace HfsReader;

/// <summary>
/// Represents an HFS folder record in the catalog.
/// </summary>
public readonly struct HfsFolderRecord
{
    /// <summary>
    /// The size of an HFS folder record in bytes.
    /// </summary>
    public const int Size = 54;

    /// <summary>
    /// Gets the catalog data record type.
    /// </summary>
    public HfsCatalogDataRecordType Type { get; }

    /// <summary>
    /// Gets the folder flags.
    /// </summary>
    public ushort FolderFlags { get; }

    /// <summary>
    /// Gets the number of directory entries (valence).
    /// </summary>
    public ushort NumberOfDirectoryEntries { get; }

    /// <summary>
    /// Gets the folder identifier (CNID).
    /// </summary>
    public uint Identifier { get; }

    /// <summary>
    /// Gets the creation date and time.
    /// </summary>
    public HfsTimestamp CreationDate { get; }

    /// <summary>
    /// Gets the content modification date and time.
    /// </summary>
    public HfsTimestamp ContentModificationDate { get; }

    /// <summary>
    /// Gets the backup date and time.
    /// </summary>
    public HfsTimestamp BackupDate { get; }

    /// <summary>
    /// Gets the folder information.
    /// </summary>
    public HfsFolderInformation FolderInformation { get; }

    /// <summary>
    /// Gets the extended folder information.
    /// </summary>
    public HfsExtendedFolderInformation ExtendedFolderInformation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HfsFolderRecord"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the folder record data.</param>
    /// <exception cref="ArgumentException">Thrown when the data length is not equal to the required size.</exception>
    public HfsFolderRecord(Span<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Folder record data must be exactly {Size} bytes long.", nameof(data));
        }

        int offset = 0;

        // Record type
        Type = (HfsCatalogDataRecordType)BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        if (Type != HfsCatalogDataRecordType.Folder)
        {
            throw new InvalidDataException("Invalid folder record type.");
        }

        // Directory (folder) flags
        // See section: directory record flags
        FolderFlags = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // Number of directory entries (valence)
        NumberOfDirectoryEntries = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // The identifier
        // Contains a CNID
        Identifier = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Creation date and time
        // Contains a HFS timestamp in local time
        CreationDate = new HfsTimestamp(data[offset..]);
        offset += HfsTimestamp.Size;

        // (content) modification date and time
        // Contains a HFS timestamp in local time
        ContentModificationDate = new HfsTimestamp(data[offset..]);
        offset += HfsTimestamp.Size;

        // Backup date and time
        // Contains a HFS timestamp in local time
        BackupDate = new HfsTimestamp(data[offset..]);
        offset += HfsTimestamp.Size;

        // Folder information
        // See section: HFS folder information
        FolderInformation = new HfsFolderInformation(data.Slice(offset, HfsFolderInformation.Size));
        offset += HfsFolderInformation.Size;

        // Extended folder information
        // See section: Hfs extended folder information
        ExtendedFolderInformation = new HfsExtendedFolderInformation(data.Slice(offset, HfsExtendedFolderInformation.Size));
        offset += HfsExtendedFolderInformation.Size;

        Debug.Assert(offset == data.Length, "Did not read the expected number of bytes for HfsFolderRecord.");
    }
}
