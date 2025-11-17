using System.Buffers.Binary;
using HfsReader.Utilities;

namespace HfsReader;

/// <summary>
/// Represents an HFS folder record in the catalog.
/// </summary>
public struct HFSFolderRecord
{
    /// <summary>Gets the catalog data record type.</summary>
    public HFSCatalogDataRecordType Type { get; }

    /// <summary>Gets the folder flags.</summary>
    public ushort FolderFlags { get; }

    /// <summary>Gets the number of directory entries (valence).</summary>
    public ushort NumberOfDirectoryEntries { get; }

    /// <summary>Gets the folder identifier (CNID).</summary>
    public uint Identifier { get; }

    /// <summary>Gets the creation date and time.</summary>
    public DateTime CreationDate { get; }

    /// <summary>Gets the content modification date and time.</summary>
    public DateTime ContentModificationDate { get; }

    /// <summary>Gets the backup date and time.</summary>
    public DateTime BackupDate { get; }

    /// <summary>Gets the folder information.</summary>
    public HFSFolderInformation FolderInformation { get; }

    /// <summary>Gets the extended folder information.</summary>
    public HFSExtendedFolderInformation ExtendedFolderInformation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HFSFolderRecord"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the folder record data.</param>
    public HFSFolderRecord(Span<byte> data)
    {
        if (data.Length < 70)
        {
            throw new ArgumentException("Folder record data must be at least 14 bytes long.", nameof(data));
        }

        int offset = 0;

        // Record type
        Type = (HFSCatalogDataRecordType)BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        if (Type != HFSCatalogDataRecordType.Folder)
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
        CreationDate = SpanUtilities.ReadHfsTimestamp(data, offset);
        offset += 4;

        // (content) modification date and time
        // Contains a HFS timestamp in local time
        ContentModificationDate = SpanUtilities.ReadHfsTimestamp(data, offset);
        offset += 4;

        // Backup date and time
        // Contains a HFS timestamp in local time
        BackupDate = SpanUtilities.ReadHfsTimestamp(data, offset);
        offset += 4;

        // Folder information
        // See section: HFS folder information
        FolderInformation = new HFSFolderInformation(data.Slice(offset, HFSFolderInformation.Size));
        offset += HFSFolderInformation.Size;

        // Extended folder information
        // See section: HFS extended folder information
        ExtendedFolderInformation = new HFSExtendedFolderInformation(data.Slice(offset, HFSExtendedFolderInformation.Size));
        offset += HFSExtendedFolderInformation.Size;
    }
}
