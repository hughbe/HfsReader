using System.Diagnostics;
using HfsReader.Utilities;

namespace HfsReader;

public struct HFSMasterDirectoryBlock
{
    public ushort Signature { get; }
    public DateTime CreationDateTime { get; }
    public DateTime ModificationDateTime { get; }
    public ushort VolumeAttributeFlags { get; }
    public ushort FileCount { get; }
    public ushort VolumeBitmapBlockNumber { get; }
    public ushort UnknownNextAllocationSearch { get; }
    public ushort NumberOfAllocationBlocks { get; }
    public uint AllocationBlockSize { get; }
    public uint DefaultClumpSize { get; }
    public ushort ExtentsStartBlockNumber { get; }
    public uint NextAvailableCatalogNodeID { get; }
    public ushort NumberOfUnusedAllocationBlocks { get; }
    public string VolumeLabel { get; }
    public DateTime BackupDateTime { get; }
    public ushort BackupSequenceNumber { get; }
    public uint VolumeWriteCount { get; }
    public uint ClumpSizeForExtentsOverflowFile { get; }
    public uint ClumpSizeForCatalogFile { get; }
    public ushort SubDirectoryCount { get; }
    public uint TotalFileCount { get; }
    public uint TotalDirectoryCount { get; }
    public HFSFinderInformation FinderInformation { get; }
    public ushort EmbeddedVolumeSignature { get; }
    public uint EmbeddedVolumeExtentDescriptor { get; }
    public uint ExtentsOverflowFileSize { get; }
    public HFSExtentRecord ExtentsOverflowExtents { get; }
    public uint CatalogFileSize { get; }
    public HFSExtentRecord CatalogFileExtents { get; }

    public HFSMasterDirectoryBlock(Span<byte> data)
    {
        if (data.Length < 512)
        {
            throw new ArgumentException("Master Directory Block data must be at least 512 bytes long.", nameof(data));
        }

        int offset = 0;

        // The volume signature (kHFSSigWord)
        // For Macintosh File System (MFS) volumes the signature contains "\xd2\xd7".
        Signature = SpanUtilities.ReadUInt16BE(data, offset);
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
        VolumeAttributeFlags = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // The number of files in the root directory
        FileCount = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // Volume bitmap block number
        // Contains an allocation block number relative from the start of the volume,
        // where 0 is the first block number.
        // Typically has a value of 3
        VolumeBitmapBlockNumber = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // Unknown (Start of the next allocation search)
        // The (allocation or volume block) index of the allocation block at which
        // the next allocation search will begin.
        UnknownNextAllocationSearch = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // Number of (allocation) blocks
        // A volume can contain at most 65535 blocks.
        NumberOfAllocationBlocks = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // Allocation block size
        // Contains number of bytes an must be a multitude of 512 bytes.
        AllocationBlockSize = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        // Default clump size
        DefaultClumpSize = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        // Extents start block number
        // Contains an allocation block number relative from the start of the
        // volume, where 0 is the first block number.
        ExtentsStartBlockNumber = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // Next available catalog node identifier (CNID)
        // Can be a directory or file record identifier.
        NextAvailableCatalogNodeID = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        // Number of unused (allocation) blocks
        NumberOfUnusedAllocationBlocks = SpanUtilities.ReadUInt16BE(data, offset);
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
        BackupSequenceNumber = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // Volume write count
        // Contains the number of times the volume has been written to.
        VolumeWriteCount = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        // Clump size for extents (overflow) file
        ClumpSizeForExtentsOverflowFile = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        // Clump size for catalog file
        ClumpSizeForCatalogFile = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        // The number of sub directories in the root directory
        SubDirectoryCount = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // Total number of files
        // It should equal the number of file records found in the catalog file.
        TotalFileCount = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        // Total number of directories (folders)
        // The value does not include the root folder.
        // It should equal the number of folder records in the catalog file minus one.
        TotalDirectoryCount = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        // Finder information
        // See section: Finder information
        FinderInformation = new HFSFinderInformation(data.Slice(offset, 32));
        offset += 32;

        // Embedded volume signature (formerly drVCSize)
        EmbeddedVolumeSignature = SpanUtilities.ReadUInt16BE(data, offset);
        offset += 2;

        // Embedded volume extent descriptor (formerly drVBMCSize and drCtlCSize)
        // Contains a single HFS extent descriptor
        EmbeddedVolumeExtentDescriptor = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        // Extents (overflow) file size
        ExtentsOverflowFileSize = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        // Extents (overflow) extents record
        // See section: The HFS extents record
        ExtentsOverflowExtents = new HFSExtentRecord(data.Slice(offset, HFSExtentRecord.Size));
        offset += HFSExtentRecord.Size;

        // Catalog file size
        CatalogFileSize = SpanUtilities.ReadUInt32BE(data, offset);
        offset += 4;

        // Catalog file extents record
        // See section: The HFS extents record
        CatalogFileExtents = new HFSExtentRecord(data.Slice(offset, HFSExtentRecord.Size));
        offset += HFSExtentRecord.Size;

        Debug.Assert(offset <= data.Length);
    }
}
