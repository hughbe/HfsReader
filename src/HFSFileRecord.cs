using System.Diagnostics;
using HfsReader.Utilities;

namespace HfsReader;

public struct HFSFileRecord
{
    public const int Size = 102;

    public HFSCatalogDataRecordType Type { get; }

    public HFSFileRecordFlags Flags { get; }

    public byte FileType { get; }

    public HFSFileInformation FileInformation { get; }

    public uint Identifier { get; }

    public ushort DataForkBlockNumber { get; }

    public uint DataForkSize { get; }

    public uint DataForkAllocatedSize { get; }

    public ushort ResourceForkBlockNumber { get; }

    public uint ResourceForkSize { get; }

    public uint ResourceForkAllocatedSize { get; }

    public DateTime CreationDate { get; }

    public DateTime ModificationDate { get; }

    public DateTime BackupDate { get; }

    public HFSExtendedFileInformation ExtendedFileInformation { get; }

    public ushort ClumpSize { get; }

    public HFSExtentRecord FirstDataForkExtents { get; }

    public HFSExtentRecord FirstResourceForkExtents { get; }

    public uint Reserved { get; }

    public HFSFileRecord(Span<byte> data)
    {
        if (data.Length < Size)
        {
            throw new ArgumentException($"File record data must be at least {Size} bytes long.", nameof(data));
        }

        int offset = 0;

        // The record type
        Type = (HFSCatalogDataRecordType)SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // Flags
        // Signed 8-bit integer
        // See section: file record flags
        Flags = (HFSFileRecordFlags)data[offset];
        offset += 1;

        // File type
        // Signed 8-bit integer
        // This field should always contain 0.
        FileType = data[offset];
        offset += 1;

        // File information
        // See section: HFS file information
        FileInformation = new HFSFileInformation(data.Slice(offset, HFSFileInformation.Size));
        offset += HFSFileInformation.Size;

        // The identifier
        // Contains a CNID
        Identifier = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        // Data fork block number (not used?)
        DataForkBlockNumber = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // Data fork size
        DataForkSize = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        // Data fork allocated size
        DataForkAllocatedSize = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        // Resource fork block number (not used?)
        ResourceForkBlockNumber = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // Resource fork size
        ResourceForkSize = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        // Resource fork allocated size
        ResourceForkAllocatedSize = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        // Creation date and time
        // Contains a HFS timestamp in local time
        CreationDate = SpanUtilities.ReadHfsTimestamp(data, offset);
        offset += 4;

        // (content) modification date and time
        // Contains a HFS timestamp in local time
        ModificationDate = SpanUtilities.ReadHfsTimestamp(data, offset);
        offset += 4;

        // Backup date and time
        // Contains a HFS timestamp in local time
        BackupDate = SpanUtilities.ReadHfsTimestamp(data, offset);
        offset += 4;

        // Extended file information
        ExtendedFileInformation = new HFSExtendedFileInformation(data.Slice(offset, HFSExtendedFileInformation.Size));
        offset += HFSExtendedFileInformation.Size;

        // The clump size
        ClumpSize = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // The first data fork extents record
        // See section: The HFS extents record
        FirstDataForkExtents = new HFSExtentRecord(data.Slice(offset, HFSExtentRecord.Size));
        offset += HFSExtentRecord.Size;

        // The first resource fork extents record
        // See section: The HFS extents record
        FirstResourceForkExtents = new HFSExtentRecord(data.Slice(offset, HFSExtentRecord.Size));
        offset += HFSExtentRecord.Size;

        // Unknown (Reserved)
        Reserved = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        Debug.Assert(offset == Size);
    }
}