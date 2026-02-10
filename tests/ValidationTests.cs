using HfsReader.Utilities;

namespace HfsReader.Tests;

public class ValidationTests
{
    #region HfsDisk

    [Fact]
    public void HfsDisk_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("stream", () => new HfsDisk(null!));
    }

    [Fact]
    public void HfsDisk_NonSeekableStream_ThrowsArgumentException()
    {
        using var stream = new NonSeekableStream();
        Assert.Throws<ArgumentException>("stream", () => new HfsDisk(stream));
    }

    [Fact]
    public void HfsDisk_NonReadableStream_ThrowsArgumentException()
    {
        using var stream = new NonReadableStream();
        Assert.Throws<ArgumentException>("stream", () => new HfsDisk(stream));
    }

    #endregion

    #region HfsMasterDirectoryBlock

    [Fact]
    public void HfsMasterDirectoryBlock_DataTooShort_ThrowsArgumentException()
    {
        var data = new byte[511];
        Assert.Throws<ArgumentException>("data", () => new HfsMasterDirectoryBlock(data));
    }

    [Fact]
    public void HfsMasterDirectoryBlock_InvalidSignature_ThrowsInvalidDataException()
    {
        var data = new byte[512];
        // Set an invalid signature (0x0000)
        data[0] = 0x00;
        data[1] = 0x00;
        Assert.Throws<InvalidDataException>(() => new HfsMasterDirectoryBlock(data));
    }

    [Fact]
    public void HfsMasterDirectoryBlock_MfsSignature_ThrowsInvalidDataException()
    {
        var data = new byte[512];
        // Set MFS signature (0xD2D7)
        data[0] = 0xD2;
        data[1] = 0xD7;
        var ex = Assert.Throws<InvalidDataException>(() => new HfsMasterDirectoryBlock(data));
        Assert.Contains("Macintosh File System (MFS)", ex.Message);
    }

    [Fact]
    public void HfsMasterDirectoryBlock_ZeroAllocationBlockSize_ThrowsInvalidDataException()
    {
        var data = CreateValidMdbData();
        // Set AllocationBlockSize to 0 (offset 20, 4 bytes big-endian)
        data[20] = 0;
        data[21] = 0;
        data[22] = 0;
        data[23] = 0;
        Assert.Throws<InvalidDataException>(() => new HfsMasterDirectoryBlock(data));
    }

    [Fact]
    public void HfsMasterDirectoryBlock_NonMultipleOf512AllocationBlockSize_ThrowsInvalidDataException()
    {
        var data = CreateValidMdbData();
        // Set AllocationBlockSize to 513 (not a multiple of 512)
        data[20] = 0;
        data[21] = 0;
        data[22] = 0x02;
        data[23] = 0x01; // 513 big-endian
        Assert.Throws<InvalidDataException>(() => new HfsMasterDirectoryBlock(data));
    }

    [Fact]
    public void HfsMasterDirectoryBlock_ValidAllocationBlockSize_DoesNotThrow()
    {
        var data = CreateValidMdbData();
        var mdb = new HfsMasterDirectoryBlock(data);
        Assert.Equal(512u, mdb.AllocationBlockSize);
    }

    #endregion

    #region HfsBootBlockHeader

    [Fact]
    public void HfsBootBlockHeader_DataTooShort_ThrowsArgumentException()
    {
        var data = new byte[HfsBootBlockHeader.Size - 1];
        Assert.Throws<ArgumentException>("data", () => new HfsBootBlockHeader(data));
    }

    [Fact]
    public void HfsBootBlockHeader_InvalidSignature_ThrowsInvalidDataException()
    {
        var data = new byte[HfsBootBlockHeader.Size];
        // Invalid signature (not 0x4C4B)
        data[0] = 0x00;
        data[1] = 0x00;
        Assert.Throws<InvalidDataException>(() => new HfsBootBlockHeader(data));
    }

    [Fact]
    public void HfsBootBlockHeader_ExactSizeData_DoesNotThrow()
    {
        var data = new byte[HfsBootBlockHeader.Size];
        // Set valid signature 'LK' (0x4C4B)
        data[0] = 0x4C;
        data[1] = 0x4B;
        var header = new HfsBootBlockHeader(data);
        Assert.Equal(0x4C4Bu, header.Signature);
    }

    #endregion

    #region BTNodeDescriptor

    [Fact]
    public void BTNodeDescriptor_DataTooShort_ThrowsArgumentException()
    {
        var data = new byte[BTNodeDescriptor.Size - 1];
        Assert.Throws<ArgumentException>("data", () => new BTNodeDescriptor(data));
    }

    [Fact]
    public void BTNodeDescriptor_DataTooLong_ThrowsArgumentException()
    {
        var data = new byte[BTNodeDescriptor.Size + 1];
        Assert.Throws<ArgumentException>("data", () => new BTNodeDescriptor(data));
    }

    #endregion

    #region BTHeaderRec

    [Fact]
    public void BTHeaderRec_DataTooShort_ThrowsArgumentException()
    {
        var data = new byte[BTHeaderRec.Size - 1];
        Assert.Throws<ArgumentException>("data", () => new BTHeaderRec(data));
    }

    [Fact]
    public void BTHeaderRec_DataTooLong_ThrowsArgumentException()
    {
        var data = new byte[BTHeaderRec.Size + 1];
        Assert.Throws<ArgumentException>("data", () => new BTHeaderRec(data));
    }

    #endregion

    #region HfsFileRecord

    [Fact]
    public void HfsFileRecord_DataTooShort_ThrowsArgumentException()
    {
        var data = new byte[HfsFileRecord.Size - 1];
        Assert.Throws<ArgumentException>("data", () => new HfsFileRecord(data));
    }

    [Fact]
    public void HfsFileRecord_DataTooLong_ThrowsArgumentException()
    {
        var data = new byte[HfsFileRecord.Size + 1];
        Assert.Throws<ArgumentException>("data", () => new HfsFileRecord(data));
    }

    #endregion

    #region HfsFolderRecord

    [Fact]
    public void HfsFolderRecord_DataTooShort_ThrowsArgumentException()
    {
        var data = new byte[HfsFolderRecord.Size - 1];
        Assert.Throws<ArgumentException>("data", () => new HfsFolderRecord(data));
    }

    [Fact]
    public void HfsFolderRecord_DataTooLong_ThrowsArgumentException()
    {
        var data = new byte[HfsFolderRecord.Size + 1];
        Assert.Throws<ArgumentException>("data", () => new HfsFolderRecord(data));
    }

    [Fact]
    public void HfsFolderRecord_InvalidRecordType_ThrowsInvalidDataException()
    {
        var data = new byte[HfsFolderRecord.Size];
        // Set record type to File (0x0200) instead of Folder (0x0100)
        data[0] = 0x02;
        data[1] = 0x00;
        Assert.Throws<InvalidDataException>(() => new HfsFolderRecord(data));
    }

    #endregion

    #region HfsExtentRecord

    [Fact]
    public void HfsExtentRecord_DataTooShort_ThrowsArgumentException()
    {
        var data = new byte[HfsExtentRecord.Size - 1];
        Assert.Throws<ArgumentException>("data", () => new HfsExtentRecord(data));
    }

    [Fact]
    public void HfsExtentRecord_DataTooLong_ThrowsArgumentException()
    {
        var data = new byte[HfsExtentRecord.Size + 1];
        Assert.Throws<ArgumentException>("data", () => new HfsExtentRecord(data));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(100)]
    public void HfsExtentRecord_IndexerOutOfRange_ThrowsIndexOutOfRangeException(int index)
    {
        var data = new byte[HfsExtentRecord.Size];
        var record = new HfsExtentRecord(data);
        Assert.Throws<IndexOutOfRangeException>(() => record[index]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void HfsExtentRecord_ValidIndex_DoesNotThrow(int index)
    {
        var data = new byte[HfsExtentRecord.Size];
        var record = new HfsExtentRecord(data);
        var _ = record[index]; // Should not throw
    }

    #endregion

    #region HfsExtentDescriptor

    [Fact]
    public void HfsExtentDescriptor_DataTooShort_ThrowsArgumentException()
    {
        var data = new byte[HfsExtentDescriptor.Size - 1];
        Assert.Throws<ArgumentException>("data", () => new HfsExtentDescriptor(data));
    }

    [Fact]
    public void HfsExtentDescriptor_DataTooLong_ThrowsArgumentException()
    {
        var data = new byte[HfsExtentDescriptor.Size + 1];
        Assert.Throws<ArgumentException>("data", () => new HfsExtentDescriptor(data));
    }

    #endregion

    #region HfsExtentsKey

    [Fact]
    public void HfsExtentsKey_DataTooShort_ThrowsArgumentException()
    {
        var data = new byte[HfsExtentsKey.Size - 1];
        Assert.Throws<ArgumentException>("data", () => new HfsExtentsKey(data));
    }

    [Fact]
    public void HfsExtentsKey_DataTooLong_ThrowsArgumentException()
    {
        var data = new byte[HfsExtentsKey.Size + 1];
        Assert.Throws<ArgumentException>("data", () => new HfsExtentsKey(data));
    }

    [Fact]
    public void HfsExtentsKey_InvalidKeyLength_ThrowsArgumentException()
    {
        var data = new byte[HfsExtentsKey.Size];
        // Key length byte should be 7; set it to something else
        data[0] = 6;
        Assert.Throws<ArgumentException>("data", () => new HfsExtentsKey(data));
    }

    [Fact]
    public void HfsExtentsKey_ValidKeyLength_DoesNotThrow()
    {
        var data = new byte[HfsExtentsKey.Size];
        data[0] = 7; // Valid key length
        var key = new HfsExtentsKey(data);
        Assert.Equal(7, key.KeyLength);
    }

    #endregion

    #region HfsCatalogKey

    [Fact]
    public void HfsCatalogKey_EmptyData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () =>
        {
            ReadOnlySpan<byte> data = [];
            _ = new HfsCatalogKey(data, out _);
        });
    }

    [Fact]
    public void HfsCatalogKey_KeySizeTooSmall_ThrowsInvalidDataException()
    {
        // KeySize of 5 is below the minimum of 6
        var data = new byte[] { 5 };
        Assert.Throws<InvalidDataException>(() => new HfsCatalogKey(data, out _));
    }

    [Fact]
    public void HfsCatalogKey_KeySizeTooLarge_ThrowsInvalidDataException()
    {
        // KeySize of 38 exceeds the maximum of 37
        var data = new byte[] { 38 };
        Assert.Throws<InvalidDataException>(() => new HfsCatalogKey(data, out _));
    }

    [Fact]
    public void HfsCatalogKey_KeySizeExceedsData_ThrowsInvalidDataException()
    {
        // KeySize of 10 but only 7 bytes of data available (1 byte key size + 6 bytes)
        var data = new byte[] { 10, 0, 0, 0, 0, 0, 0 };
        Assert.Throws<InvalidDataException>(() => new HfsCatalogKey(data, out _));
    }

    [Fact]
    public void HfsCatalogKey_ZeroKeySize_DoesNotThrow()
    {
        // KeySize of 0 means empty key - should not throw
        var data = new byte[] { 0 };
        var key = new HfsCatalogKey(data, out int bytesRead);
        Assert.Equal(0, key.KeySize);
        Assert.Equal(1, bytesRead);
    }

    [Fact]
    public void HfsCatalogKey_ValidMinimalKey_DoesNotThrow()
    {
        // KeySize of 6: 1 reserved + 4 parent ID + 1 name length (0)
        var data = new byte[8]; // 1 key size + 7 bytes of key data
        data[0] = 6; // key size
        // Reserved, ParentID, NameLength all zero
        var key = new HfsCatalogKey(data, out int bytesRead);
        Assert.Equal(6, key.KeySize);
        Assert.Equal(7, bytesRead); // 1 + KeySize
    }

    #endregion

    #region HfsThreadRecord

    [Fact]
    public void HfsThreadRecord_DataTooShort_ThrowsArgumentException()
    {
        var data = new byte[HfsThreadRecord.MinSize - 1];
        Assert.Throws<ArgumentException>("data", () => new HfsThreadRecord(data));
    }

    [Fact]
    public void HfsThreadRecord_InvalidRecordType_ThrowsArgumentException()
    {
        var data = new byte[HfsThreadRecord.MinSize];
        // Set type to File (0x0200) instead of FileThread or FolderThread
        data[0] = 0x02;
        data[1] = 0x00;
        Assert.Throws<ArgumentException>("data", () => new HfsThreadRecord(data));
    }

    [Fact]
    public void HfsThreadRecord_ValidFolderThread_DoesNotThrow()
    {
        var data = new byte[HfsThreadRecord.MinSize];
        // FolderThread = 0x0300
        data[0] = 0x03;
        data[1] = 0x00;
        // NameLength = 0 (at offset 14)
        data[14] = 0;
        var record = new HfsThreadRecord(data);
        Assert.Equal(HfsCatalogDataRecordType.FolderThread, record.Type);
    }

    [Fact]
    public void HfsThreadRecord_ValidFileThread_DoesNotThrow()
    {
        var data = new byte[HfsThreadRecord.MinSize];
        // FileThread = 0x0400
        data[0] = 0x04;
        data[1] = 0x00;
        // NameLength = 0
        data[14] = 0;
        var record = new HfsThreadRecord(data);
        Assert.Equal(HfsCatalogDataRecordType.FileThread, record.Type);
    }

    #endregion

    #region HfsFileInformation

    [Fact]
    public void HfsFileInformation_DataTooShort_ThrowsArgumentException()
    {
        var data = new byte[HfsFileInformation.Size - 1];
        Assert.Throws<ArgumentException>("data", () => new HfsFileInformation(data));
    }

    [Fact]
    public void HfsFileInformation_DataTooLong_ThrowsArgumentException()
    {
        var data = new byte[HfsFileInformation.Size + 1];
        Assert.Throws<ArgumentException>("data", () => new HfsFileInformation(data));
    }

    #endregion

    #region HfsFolderInformation

    [Fact]
    public void HfsFolderInformation_DataTooShort_ThrowsArgumentException()
    {
        var data = new byte[HfsFolderInformation.Size - 1];
        Assert.Throws<ArgumentException>("data", () => new HfsFolderInformation(data));
    }

    [Fact]
    public void HfsFolderInformation_DataTooLong_ThrowsArgumentException()
    {
        var data = new byte[HfsFolderInformation.Size + 1];
        Assert.Throws<ArgumentException>("data", () => new HfsFolderInformation(data));
    }

    #endregion

    #region HfsExtendedFileInformation

    [Fact]
    public void HfsExtendedFileInformation_DataTooShort_ThrowsArgumentException()
    {
        var data = new byte[HfsExtendedFileInformation.Size - 1];
        Assert.Throws<ArgumentException>("data", () => new HfsExtendedFileInformation(data));
    }

    [Fact]
    public void HfsExtendedFileInformation_DataTooLong_ThrowsArgumentException()
    {
        var data = new byte[HfsExtendedFileInformation.Size + 1];
        Assert.Throws<ArgumentException>("data", () => new HfsExtendedFileInformation(data));
    }

    #endregion

    #region HfsExtendedFolderInformation

    [Fact]
    public void HfsExtendedFolderInformation_DataTooShort_ThrowsArgumentException()
    {
        var data = new byte[HfsExtendedFolderInformation.Size - 1];
        Assert.Throws<ArgumentException>("data", () => new HfsExtendedFolderInformation(data));
    }

    [Fact]
    public void HfsExtendedFolderInformation_DataTooLong_ThrowsArgumentException()
    {
        var data = new byte[HfsExtendedFolderInformation.Size + 1];
        Assert.Throws<ArgumentException>("data", () => new HfsExtendedFolderInformation(data));
    }

    #endregion

    #region HfsFinderInformation

    [Fact]
    public void HfsFinderInformation_DataTooShort_ThrowsArgumentException()
    {
        var data = new byte[HfsFinderInformation.Size - 1];
        Assert.Throws<ArgumentException>("data", () => new HfsFinderInformation(data));
    }

    [Fact]
    public void HfsFinderInformation_DataTooLong_ThrowsArgumentException()
    {
        var data = new byte[HfsFinderInformation.Size + 1];
        Assert.Throws<ArgumentException>("data", () => new HfsFinderInformation(data));
    }

    #endregion

    #region String Types

    [Fact]
    public void String16_DataTooLong_ThrowsArgumentException()
    {
        var data = new byte[String16.Size + 1];
        Assert.Throws<ArgumentException>("data", () => new String16(data));
    }

    [Fact]
    public void String27_DataTooLong_ThrowsArgumentException()
    {
        var data = new byte[String27.Size + 1];
        Assert.Throws<ArgumentException>("data", () => new String27(data));
    }

    [Fact]
    public void String31_DataTooLong_ThrowsArgumentException()
    {
        var data = new byte[String31.Size + 1];
        Assert.Throws<ArgumentException>("data", () => new String31(data));
    }

    #endregion

    #region HfsVolume Public Method Validation

    [Fact]
    public void HfsVolume_ContentsOfDirectory_NullDirectory_ThrowsArgumentNullException()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "dropstuff_40.dsk"));
        var disk = new HfsDisk(stream);
        var volume = disk.Volumes[0];

        Assert.Throws<ArgumentNullException>("directory", () => volume.ContentsOfDirectory(null!));
    }

    [Fact]
    public void HfsVolume_GetFileData_NullFile_ThrowsArgumentNullException()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "dropstuff_40.dsk"));
        var disk = new HfsDisk(stream);
        var volume = disk.Volumes[0];

        Assert.Throws<ArgumentNullException>("file", () => volume.GetFileData(null!, HfsForkType.DataFork));
    }

    [Fact]
    public void HfsVolume_GetFileData_NullOutputStream_ThrowsArgumentNullException()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "dropstuff_40.dsk"));
        var disk = new HfsDisk(stream);
        var volume = disk.Volumes[0];
        var file = volume.RootContents()
            .SelectMany(n => n is HfsDirectory d ? volume.ContentsOfDirectory(d) : [n])
            .OfType<HfsFile>()
            .First();

        Assert.Throws<ArgumentNullException>("outputStream", () => volume.GetFileData(file, null!, HfsForkType.DataFork));
    }

    [Fact]
    public void HfsVolume_GetFileData_InvalidForkType_ThrowsArgumentOutOfRangeException()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "dropstuff_40.dsk"));
        var disk = new HfsDisk(stream);
        var volume = disk.Volumes[0];
        var file = volume.RootContents()
            .SelectMany(n => n is HfsDirectory d ? volume.ContentsOfDirectory(d) : [n])
            .OfType<HfsFile>()
            .First();

        Assert.Throws<ArgumentOutOfRangeException>("forkType", () => volume.GetFileData(file, new MemoryStream(), (HfsForkType)99));
    }

    [Fact]
    public void HfsVolume_GetDataForkData_NullFile_ThrowsArgumentNullException()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "dropstuff_40.dsk"));
        var disk = new HfsDisk(stream);
        var volume = disk.Volumes[0];

        Assert.Throws<ArgumentNullException>("file", () => volume.GetDataForkData(null!));
    }

    [Fact]
    public void HfsVolume_GetResourceForkData_NullFile_ThrowsArgumentNullException()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "dropstuff_40.dsk"));
        var disk = new HfsDisk(stream);
        var volume = disk.Volumes[0];

        Assert.Throws<ArgumentNullException>("file", () => volume.GetResourceForkData(null!));
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Creates a minimal valid MDB byte array with the correct signature and allocation block size.
    /// </summary>
    private static byte[] CreateValidMdbData()
    {
        var data = new byte[512];
        // Signature 'BD' (0x4244)
        data[0] = 0x42;
        data[1] = 0x44;
        // AllocationBlockSize at offset 20 = 512 (0x00000200)
        data[20] = 0x00;
        data[21] = 0x00;
        data[22] = 0x02;
        data[23] = 0x00;
        return data;
    }

    /// <summary>
    /// A stream wrapper that does not support seeking.
    /// </summary>
    private sealed class NonSeekableStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;
        public override long Position { get => 0; set { } }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>
    /// A stream wrapper that does not support reading.
    /// </summary>
    private sealed class NonReadableStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position { get => 0; set { } }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => 0;
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
    }

    #endregion
}
