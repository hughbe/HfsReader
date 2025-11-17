using System.Buffers.Binary;
using System.Diagnostics;
using HfsReader.Utilities;

namespace HfsReader;

/// <summary>
/// Represents the master directory block of an HFS volume.
/// </summary>
public struct HFSMasterDirectoryBlock
{
    /// <summary>Gets the volume signature.</summary>
    public ushort Signature { get; }
    /// <summary>Gets the creation date and time of the volume.</summary>
    public DateTime CreationDateTime { get; }
    /// <summary>Gets the modification date and time of the volume.</summary>
    public DateTime ModificationDateTime { get; }
    /// <summary>Gets the volume attribute flags.</summary>
    public ushort VolumeAttributeFlags { get; }
    /// <summary>Gets the number of files in the root directory.</summary>
    public ushort FileCount { get; }
    /// <summary>Gets the volume bitmap block number.</summary>
    public ushort VolumeBitmapBlockNumber { get; }
    /// <summary>Gets the start of the next allocation search.</summary>
    public ushort NextAllocationSearch { get; }
    /// <summary>Gets the number of allocation blocks.</summary>
    public ushort NumberOfAllocationBlocks { get; }
    /// <summary>Gets the allocation block size in bytes.</summary>
    public uint AllocationBlockSize { get; }
    /// <summary>Gets the default clump size.</summary>
    public uint DefaultClumpSize { get; }
    /// <summary>Gets the extents start block number.</summary>
    public ushort ExtentsStartBlockNumber { get; }
    /// <summary>Gets the next available catalog node identifier (CNID).</summary>
    public uint NextAvailableCatalogNodeID { get; }
    /// <summary>Gets the number of unused allocation blocks.</summary>
    public ushort NumberOfUnusedAllocationBlocks { get; }
    /// <summary>Gets the volume label.</summary>
    public string VolumeLabel { get; }
    /// <summary>Gets the backup date and time of the volume.</summary>
    public DateTime BackupDateTime { get; }
    /// <summary>Gets the backup sequence number.</summary>
    public ushort BackupSequenceNumber { get; }
    /// <summary>Gets the volume write count.</summary>
    public uint VolumeWriteCount { get; }
    /// <summary>Gets the clump size for the extents overflow file.</summary>
    public uint ClumpSizeForExtentsOverflowFile { get; }
    /// <summary>Gets the clump size for the catalog file.</summary>
    public uint ClumpSizeForCatalogFile { get; }
    /// <summary>Gets the number of subdirectories in the root directory.</summary>
    public ushort SubDirectoryCount { get; }
    /// <summary>Gets the total number of files.</summary>
    public uint TotalFileCount { get; }
    /// <summary>Gets the total number of directories (excluding the root).</summary>
    public uint TotalDirectoryCount { get; }
    /// <summary>Gets the Finder information for the volume.</summary>
    public HFSFinderInformation FinderInformation { get; }
    /// <summary>Gets the embedded volume signature.</summary>
    public ushort EmbeddedVolumeSignature { get; }
    /// <summary>Gets the embedded volume extent descriptor.</summary>
    public uint EmbeddedVolumeExtentDescriptor { get; }
    /// <summary>Gets the extents overflow file size.</summary>
    public uint ExtentsOverflowFileSize { get; }
    /// <summary>Gets the extents overflow extents record.</summary>
    public HFSExtentRecord ExtentsOverflowExtents { get; }
    /// <summary>Gets the catalog file size.</summary>
    public uint CatalogFileSize { get; }
    /// <summary>Gets the catalog file extents record.</summary>
    public HFSExtentRecord CatalogFileExtents { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HFSMasterDirectoryBlock"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the master directory block data.</param>
    public HFSMasterDirectoryBlock(Span<byte> data)
    {
        if (data.Length < 512)
        {
            throw new ArgumentException("Master Directory Block data must be at least 512 bytes long.", nameof(data));
        }

        int offset = 0;

        // The volume signature (kHFSSigWord)
        // For Macintosh File System (MFS) volumes the signature contains "\xd2\xd7".
        Signature = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        if (Signature != 0x4244) // 'BD'
        {
            throw new InvalidDataException("Invalid DSK master directory block signature.");
        }

        // Volume creation date and time
        // Contains a HFS timestamp in local time.
        // The date and time when the volume was created.
        CreationDateTime = SpanUtilities.ReadHfsTimestamp(data, offset);
        offset += 4;

        // Volume modification date and time
        // Contains a HFS timestamp in local time.
        // The date and time when the volume was last modified. This is not necessarily
        // the data and time when the volume was last flushed.
        ModificationDateTime = SpanUtilities.ReadHfsTimestamp(data, offset);
        offset += 4;

        // Volume attribute flags
        // See section: Volume attribute flags
        VolumeAttributeFlags = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // The number of files in the root directory
        FileCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // Volume bitmap block number
        // Contains an allocation block number relative from the start of the volume,
        // where 0 is the first block number.
        // Typically has a value of 3
        VolumeBitmapBlockNumber = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // Unknown (Start of the next allocation search)
        // The (allocation or volume block) index of the allocation block at which
        // the next allocation search will begin.
        NextAllocationSearch = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // Number of (allocation) blocks
        // A volume can contain at most 65535 blocks.
        NumberOfAllocationBlocks = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // Allocation block size
        // Contains number of bytes an must be a multitude of 512 bytes.
        AllocationBlockSize = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        if (AllocationBlockSize % 512 != 0)
        {
            throw new InvalidDataException("Invalid allocation block size in master directory block.");
        }

        // Default clump size
        DefaultClumpSize = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Extents start block number
        // Contains an allocation block number relative from the start of the
        // volume, where 0 is the first block number.
        ExtentsStartBlockNumber = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // Next available catalog node identifier (CNID)
        // Can be a directory or file record identifier.
        NextAvailableCatalogNodeID = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Number of unused (allocation) blocks
        NumberOfUnusedAllocationBlocks = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // The volume label size
        // The maximum size is 27
        // The volume label
        // Contains an ASCII string
        VolumeLabel = SpanUtilities.ReadFixedLengthStringWithLength(data, offset, 27);
        offset += 28;

        // Backup date and time
        // Contains a HFS timestamp in local time
        // The date and time when the volume was last backed up.
        BackupDateTime = SpanUtilities.ReadHfsTimestamp(data, offset);
        offset += 4;

        // Backup sequence number
        BackupSequenceNumber = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // Volume write count
        // Contains the number of times the volume has been written to.
        VolumeWriteCount = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Clump size for extents (overflow) file
        ClumpSizeForExtentsOverflowFile = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Clump size for catalog file
        ClumpSizeForCatalogFile = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // The number of sub directories in the root directory
        SubDirectoryCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // Total number of files
        // It should equal the number of file records found in the catalog file.
        TotalFileCount = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Total number of directories (folders)
        // The value does not include the root folder.
        // It should equal the number of folder records in the catalog file minus one.
        TotalDirectoryCount = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Finder information
        // See section: Finder information
        FinderInformation = new HFSFinderInformation(data.Slice(offset, 32));
        offset += 32;

        // Embedded volume signature (formerly drVCSize)
        EmbeddedVolumeSignature = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // Embedded volume extent descriptor (formerly drVBMCSize and drCtlCSize)
        // Contains a single HFS extent descriptor
        EmbeddedVolumeExtentDescriptor = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Extents (overflow) file size
        ExtentsOverflowFileSize = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Extents (overflow) extents record
        // See section: The HFS extents record
        ExtentsOverflowExtents = new HFSExtentRecord(data.Slice(offset, HFSExtentRecord.Size));
        offset += HFSExtentRecord.Size;

        // Catalog file size
        CatalogFileSize = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Catalog file extents record
        // See section: The HFS extents record
        CatalogFileExtents = new HFSExtentRecord(data.Slice(offset, HFSExtentRecord.Size));
        offset += HFSExtentRecord.Size;

        Debug.Assert(offset <= data.Length);
    }
}
