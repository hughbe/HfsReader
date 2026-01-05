using System.Buffers.Binary;
using System.Diagnostics;

namespace HfsReader;

/// <summary>
/// Represents a key in the Extents Overflow B-tree.
/// </summary>
public struct HFSExtentKey
{
    /// <summary>
    /// The size of an HFS extent key in bytes (including the key length byte).
    /// </summary>
    public const int Size = 8;

    /// <summary>
    /// Gets the size of the key data (not including the length byte itself).
    /// </summary>
    public byte KeyLength { get; }

    /// <summary>
    /// Gets the fork type (0x00 = data fork, 0xFF = resource fork).
    /// </summary>
    public HFSForkType ForkType { get; }

    /// <summary>
    /// Gets the file identifier (CNID).
    /// </summary>
    public uint FileID { get; }

    /// <summary>
    /// Gets the starting allocation block number within the file's fork.
    /// </summary>
    public ushort StartBlock { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HFSExtentKey"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the extent key data.</param>
    /// <exception cref="ArgumentException">Thrown when the data length is not equal to the required size.</exception>
    public HFSExtentKey(Span<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data length must be exactly {Size} bytes to read an HFSExtentKey.", nameof(data));
        }

        // 8.1.1. The HFS extent key (record)
        // The HFS extent key (record) is 8 bytes in size and consists of:
        int offset = 0;

        // Key byte size
        // Signed 8-bit integer
        KeyLength = data[offset];
        offset += 1;

        if (KeyLength != 7)
        {
            throw new ArgumentException("Invalid HFSExtentKey length byte; expected value is 7.", nameof(data));
        }

        // Fork type
        // Signed 8-bit integer
        // See section: HFS fork types
        ForkType = (HFSForkType)data[offset];
        offset += 1;

        // File identifier
        // Contains a CNID
        FileID = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Start block
        // The first allocation block index described by the corresponding extent record
        StartBlock = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        Debug.Assert(offset == data.Length, "Did not read exactly the expected number of bytes for HFSExtentKey.");
    }

    /// <summary>
    /// Compares this key to the specified file ID, fork type, and start block.
    /// </summary>
    /// <param name="fileID">The file identifier to compare.</param>
    /// <param name="forkType">The fork type to compare.</param>
    /// <param name="startBlock">The starting block to compare.</param>
    /// <returns>An integer indicating the relative order.</returns>
    /// <remarks>
    /// According to the HFS specification, extents overflow keys are sorted by:
    /// 1. File ID (primary)
    /// 2. Fork type (secondary)
    /// 3. Start block (tertiary)
    /// </remarks>
    public readonly int CompareTo(uint fileID, HFSForkType forkType, ushort startBlock)
    {
        // Compare file ID first (primary sort key)
        int fileComparison = FileID.CompareTo(fileID);
        if (fileComparison != 0)
        {
            return fileComparison;
        }

        // Then compare fork type (secondary sort key)
        int forkComparison = ((byte)ForkType).CompareTo((byte)forkType);
        if (forkComparison != 0)
        {
            return forkComparison;
        }

        // Finally compare start block (tertiary sort key)
        return StartBlock.CompareTo(startBlock);
    }
}
