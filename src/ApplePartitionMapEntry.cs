using System.Buffers.Binary;
using System.Diagnostics;
using HfsReader.Utilities;

namespace HfsReader;

/// <summary>
/// Represents an entry in the Apple Partition Map.
/// </summary>
public struct ApplePartitionMapEntry
{
    /// <summary>
    /// The size of an Apple Partition Map Entry in bytes.
    /// </summary>
    public const int Size = 136;
    
    /// <summary>
    /// The expected block signature value ('PM').
    /// </summary>
    public const int BlockSignature = 0x504D; // 'PM'

    /// <summary>Gets the partition map entry signature.</summary>
    public ushort Signature { get; }

    /// <summary>Gets the first reserved field.</summary>
    public ushort Reserved1 { get; }

    /// <summary>Gets the total number of entries in the partition map.</summary>
    public uint MapEntryCount { get; }
    
    /// <summary>Gets the partition start sector.</summary>
    public uint PartitionStartBlock { get; }

    /// <summary>Gets the partition number of sectors.</summary>
    public uint PartitionBlockCount { get; }

    /// <summary>Gets the partition name.</summary>
    public string Name { get; }

    /// <summary>Gets the partition type.</summary>
    public string Type { get; }

    /// <summary>Gets the data area start sector.</summary>
    public uint DataStartBlock { get; }

    /// <summary>Gets the data area number of sectors.</summary>
    public uint DataBlockCount { get; }

    /// <summary>Gets the status flags.</summary>
    public uint StatusFlags { get; }

    /// <summary>Gets the boot code start sector.</summary>
    public uint BootCodeStartBlock { get; }

    /// <summary>Gets the boot code number of sectors.</summary>
    public uint BootCodeBlockCount { get; }

    /// <summary>Gets the boot code address.</summary>
    public uint BootCodeAddress { get; }

    /// <summary>Gets the second reserved field.</summary>
    public uint Reserved2 { get; }

    /// <summary>Gets the boot code entry point.</summary>
    public uint BootCodeEntryPoint { get; }

    /// <summary>Gets the third reserved field.</summary>
    public uint Reserved3 { get; }

    /// <summary>Gets the boot code checksum.</summary>
    public uint BootCodeChecksum { get; }

    /// <summary>Gets the processor type.</summary>
    public string ProcessorType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplePartitionMapEntry"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the partition map entry data.</param>
    public ApplePartitionMapEntry(Span<byte> data)
    {
        if (data.Length < Size)
        {
            throw new ArgumentException($"Data span is too small to contain an Apple Partition Map Entry. Expected at least {Size} bytes, but got {data.Length} bytes.", nameof(data));
        }

        int offset = 0;

        // Signature "PM"
        Signature = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        if (Signature != 0x504D) // 'PM'
        {
            throw new InvalidDataException("Invalid Apple Partition Map Entry signature.");
        }

        // Unknown (Reserved)
        Reserved1 = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // Number of entries
        // Contains the total number of entries in the partition map
        MapEntryCount = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Partition start sector
        PartitionStartBlock = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Partition number of sectors
        PartitionBlockCount = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Partition name
        // Contains an ASCII string (with an end-of-string character?)
        Name = SpanUtilities.ReadFixedLengthString(data, offset, 32);
        offset += 32;

        // Partition type
        // Contains an ASCII string (with an end-of-string character?)
        // See section: Partition types
        Type = SpanUtilities.ReadFixedLengthString(data, offset, 32);
        offset += 32;

        // Data area start sector
        DataStartBlock = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Data area number of sectors
        DataBlockCount = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Status flags
        // See section: Status flags
        StatusFlags = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Boot code start sector
        BootCodeStartBlock = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Boot code number of sectors
        BootCodeBlockCount = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Boot code address
        BootCodeAddress = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Unknown (Reserved)
        Reserved2 = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Boot code entry point
        BootCodeEntryPoint = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Unknown (Reserved)
        Reserved3 = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Boot code checksum
        BootCodeChecksum = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Processor type
        ProcessorType = SpanUtilities.ReadFixedLengthString(data, offset, 16);
        offset += 16;

        // Remaining bytes are reserved
        Debug.Assert(offset == Size);
    }
}
