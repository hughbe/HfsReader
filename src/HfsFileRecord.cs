using System.Buffers.Binary;
using System.Diagnostics;
using HfsReader.Utilities;

namespace HfsReader;

/// <summary>
/// Represents an HFS file record in the catalog.
/// </summary>
public struct HfsFileRecord
{
    /// <summary>
    /// The size of an HFS file record in bytes.
    /// </summary>
    public const int Size = 102;

    /// <summary>
    /// Gets the catalog data record type.
    /// </summary>
    public HfsCatalogDataRecordType Type { get; }

    /// <summary>
    /// Gets the file record flags.
    /// </summary>
    public HfsFileRecordFlags Flags { get; }

    /// <summary>
    /// Gets the file type (should always be 0).
    /// </summary>
    public byte FileType { get; }

    /// <summary>
    /// Gets the file information.
    /// </summary>
    public HfsFileInformation FileInformation { get; }

    /// <summary>
    /// Gets the file identifier (CNID).
    /// </summary>
    public uint Identifier { get; }

    /// <summary>
    /// Gets the data fork block number.
    /// </summary>
    public ushort DataForkBlockNumber { get; }

    /// <summary>
    /// Gets the data fork size in bytes.
    /// </summary>
    public uint DataForkSize { get; }

    /// <summary>
    /// Gets the data fork allocated size in bytes.
    /// </summary>
    public uint DataForkAllocatedSize { get; }

    /// <summary>
    /// Gets the resource fork block number.
    /// </summary>
    public ushort ResourceForkBlockNumber { get; }

    /// <summary>
    /// Gets the resource fork size in bytes.
    /// </summary>
    public uint ResourceForkSize { get; }

    /// <summary>
    /// Gets the resource fork allocated size in bytes.
    /// </summary>
    public uint ResourceForkAllocatedSize { get; }

    /// <summary>
    /// Gets the creation date and time.
    /// </summary>
    public DateTime CreationDate { get; }

    /// <summary>
    /// Gets the modification date and time.
    /// </summary>
    public DateTime ModificationDate { get; }

    /// <summary>
    /// Gets the backup date and time.
    /// </summary>
    public DateTime BackupDate { get; }

    /// <summary>
    /// Gets the extended file information.
    /// </summary>
    public HfsExtendedFileInformation ExtendedFileInformation { get; }

    /// <summary>
    /// Gets the clump size.
    /// </summary>
    public ushort ClumpSize { get; }

    /// <summary>
    /// Gets the first data fork extents record.
    /// </summary>
    public HfsExtentRecord FirstDataForkExtents { get; }

    /// <summary>
    /// Gets the first resource fork extents record.
    /// </summary>
    public HfsExtentRecord FirstResourceForkExtents { get; }

    /// <summary>
    /// Gets the reserved field.
    /// </summary>
    public uint Reserved { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HfsFileRecord"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the file record data.</param>
    /// <exception cref="ArgumentException">Thrown when the data length is not equal to the required size.</exception>
    public HfsFileRecord(Span<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"File record data must be exactly {Size} bytes long.", nameof(data));
        }

        int offset = 0;

        // The record type
        Type = (HfsCatalogDataRecordType)BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // Flags
        // Signed 8-bit integer
        // See section: file record flags
        Flags = (HfsFileRecordFlags)data[offset];
        offset += 1;

        // File type
        // Signed 8-bit integer
        // This field should always contain 0.
        FileType = data[offset];
        offset += 1;

        // File information
        // See section: Hfs file information
        FileInformation = new HfsFileInformation(data.Slice(offset, HfsFileInformation.Size));
        offset += HfsFileInformation.Size;

        // The identifier
        // Contains a CNID
        Identifier = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Data fork block number (not used?)
        DataForkBlockNumber = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // Data fork size
        DataForkSize = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Data fork allocated size
        DataForkAllocatedSize = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Resource fork block number (not used?)
        ResourceForkBlockNumber = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // Resource fork size
        ResourceForkSize = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Resource fork allocated size
        ResourceForkAllocatedSize = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Creation date and time
        // Contains a HFS timestamp in local time
        CreationDate = SpanUtilities.ReadMacOSTimestamp(data[offset..]);
        offset += 4;

        // (content) modification date and time
        // Contains a HFS timestamp in local time
        ModificationDate = SpanUtilities.ReadMacOSTimestamp(data[offset..]);
        offset += 4;

        // Backup date and time
        // Contains a HFS timestamp in local time
        BackupDate = SpanUtilities.ReadMacOSTimestamp(data[offset..]);
        offset += 4;

        // Extended file information
        ExtendedFileInformation = new HfsExtendedFileInformation(data.Slice(offset, HfsExtendedFileInformation.Size));
        offset += HfsExtendedFileInformation.Size;

        // The clump size
        ClumpSize = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // The first data fork extents record
        // See section: The HFS extents record
        FirstDataForkExtents = new HfsExtentRecord(data.Slice(offset, HfsExtentRecord.Size));
        offset += HfsExtentRecord.Size;

        // The first resource fork extents record
        // See section: The HFS extents record
        FirstResourceForkExtents = new HfsExtentRecord(data.Slice(offset, HfsExtentRecord.Size));
        offset += HfsExtentRecord.Size;

        // Unknown (Reserved)
        Reserved = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        Debug.Assert(offset == data.Length, "Did not read the expected number of bytes for HfsFileRecord.");
    }
}