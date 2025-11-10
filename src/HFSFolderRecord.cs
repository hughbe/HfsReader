using HfsReader.Utilities;

namespace HfsReader;

public struct HFSFolderRecord
{
    public HFSCatalogDataRecordType Type { get; }

    public ushort FolderFlags { get; }

    public ushort NumberOfDirectoryEntries { get; }

    public uint Identifier { get; }

    public DateTime CreationDate { get; }

    public DateTime ContentModificationDate { get; }

    public DateTime BackupDate { get; }

    public HFSFolderInformation FolderInformation { get; }

    public HFSExtendedFolderInformation ExtendedFolderInformation { get; }

    public HFSFolderRecord(Span<byte> data)
    {
        if (data.Length < 70)
        {
            throw new ArgumentException("Folder record data must be at least 14 bytes long.", nameof(data));
        }

        int offset = 0;

        // Record type
        Type = (HFSCatalogDataRecordType)SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;
        if (Type != HFSCatalogDataRecordType.Folder)
        {
            throw new InvalidDataException("Invalid folder record type.");
        }

        // Directory (folder) flags
        // See section: directory record flags
        FolderFlags = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // Number of directory entries (valence)
        NumberOfDirectoryEntries = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // The identifier
        // Contains a CNID
        Identifier = SpanUtilities.ReadUInt32BE(data, offset);
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
