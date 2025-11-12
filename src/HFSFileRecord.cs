using System.Buffers.Binary;
using System.Diagnostics;
using HfsReader.Utilities;

namespace HfsReader;

/// <summary>
/// Represents an HFS file record in the catalog.
/// </summary>
public struct HFSFileRecord
{
    /// <summary>
    /// The size of an HFS file record in bytes.
    /// </summary>
    public const int Size = 102;

    /// <summary>Gets the catalog data record type.</summary>
    public HFSCatalogDataRecordType Type { get; }

    /// <summary>Gets the file record flags.</summary>
    public HFSFileRecordFlags Flags { get; }

    /// <summary>Gets the file type (should always be 0).</summary>
    public byte FileType { get; }

    /// <summary>Gets the file information.</summary>
    public HFSFileInformation FileInformation { get; }

    /// <summary>Gets the file identifier (CNID).</summary>
    public uint Identifier { get; }

    /// <summary>Gets the data fork block number.</summary>
    public ushort DataForkBlockNumber { get; }

    /// <summary>Gets the data fork size in bytes.</summary>
    public uint DataForkSize { get; }

    /// <summary>Gets the data fork allocated size in bytes.</summary>
    public uint DataForkAllocatedSize { get; }

    /// <summary>Gets the resource fork block number.</summary>
    public ushort ResourceForkBlockNumber { get; }

    /// <summary>Gets the resource fork size in bytes.</summary>
    public uint ResourceForkSize { get; }

    /// <summary>Gets the resource fork allocated size in bytes.</summary>
    public uint ResourceForkAllocatedSize { get; }

    /// <summary>Gets the creation date and time.</summary>
    public DateTime CreationDate { get; }

    /// <summary>Gets the modification date and time.</summary>
    public DateTime ModificationDate { get; }

    /// <summary>Gets the backup date and time.</summary>
    public DateTime BackupDate { get; }

    /// <summary>Gets the extended file information.</summary>
    public HFSExtendedFileInformation ExtendedFileInformation { get; }

    /// <summary>Gets the clump size.</summary>
    public ushort ClumpSize { get; }

    /// <summary>Gets the first data fork extents record.</summary>
    public HFSExtentRecord FirstDataForkExtents { get; }

    /// <summary>Gets the first resource fork extents record.</summary>
    public HFSExtentRecord FirstResourceForkExtents { get; }

    /// <summary>Gets the reserved field.</summary>
    public uint Reserved { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HFSFileRecord"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the file record data.</param>
    public HFSFileRecord(Span<byte> data)
    {
        if (data.Length < Size)
        {
            throw new ArgumentException($"File record data must be at least {Size} bytes long.", nameof(data));
        }

        int offset = 0;

        // The record type
        Type = (HFSCatalogDataRecordType)BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
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
        Identifier = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Data fork block number (not used?)
        DataForkBlockNumber = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        // Data fork size
        DataForkSize = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Data fork allocated size
        DataForkAllocatedSize = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Resource fork block number (not used?)
        ResourceForkBlockNumber = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        // Resource fork size
        ResourceForkSize = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Resource fork allocated size
        ResourceForkAllocatedSize = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
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
        ClumpSize = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
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
        Reserved = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        Debug.Assert(offset == Size);
    }
}