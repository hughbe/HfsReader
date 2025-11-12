using System.Buffers.Binary;
using System.Diagnostics;
using HfsReader.Utilities;

namespace HfsReader;

public struct ApplePartitionMapEntry
{
    public const int Size = 136;
    public const int BlockSignature = 0x504D; // 'PM'

    public ushort Signature { get; }

    public ushort Reserved1 { get; }

    public uint MapEntryCount { get; }
    
    public uint PartitionStartBlock { get; }

    public uint PartitionBlockCount { get; }

    public string Name { get; }

    public string Type { get; }

    public uint DataStartBlock { get; }

    public uint DataBlockCount { get; }

    public uint StatusFlags { get; }

    public uint BootCodeStartBlock { get; }

    public uint BootCodeBlockCount { get; }

    public uint BootCodeAddress { get; }

    public uint Reserved2 { get; }

    public uint BootCodeEntryPoint { get; }

    public uint Reserved3 { get; }

    public uint BootCodeChecksum { get; }

    public string ProcessorType { get; }

    public ApplePartitionMapEntry(Span<byte> data)
    {
        if (data.Length < Size)
        {
            throw new ArgumentException($"Data span is too small to contain an Apple Partition Map Entry. Expected at least {Size} bytes, but got {data.Length} bytes.", nameof(data));
        }

        int offset = 0;

        // Signature "PM"
        Signature = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;
        if (Signature != 0x504D) // 'PM'
        {
            throw new InvalidDataException("Invalid Apple Partition Map Entry signature.");
        }

        // Unknown (Reserved)
        Reserved1 = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        // Number of entries
        // Contains the total number of entries in the partition map
        MapEntryCount = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Partition start sector
        PartitionStartBlock = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Partition number of sectors
        PartitionBlockCount = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
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
        DataStartBlock = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Data area number of sectors
        DataBlockCount = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Status flags
        // See section: Status flags
        StatusFlags = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Boot code start sector
        BootCodeStartBlock = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Boot code number of sectors
        BootCodeBlockCount = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Boot code address
        BootCodeAddress = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Unknown (Reserved)
        Reserved2 = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Boot code entry point
        BootCodeEntryPoint = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Unknown (Reserved)
        Reserved3 = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Boot code checksum
        BootCodeChecksum = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Processor type
        ProcessorType = SpanUtilities.ReadFixedLengthString(data, offset, 16);
        offset += 16;

        // Remaining bytes are reserved
        Debug.Assert(offset == Size);
    }
}
