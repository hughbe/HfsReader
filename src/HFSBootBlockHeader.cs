using System.Buffers.Binary;
using System.Diagnostics;
using HfsReader.Utilities;

namespace HfsReader;

public struct HFSBootBlockHeader
{
    public const int Size = 148;

    public ushort Signature { get; }
    public uint EntryPoint { get; }
    public ushort Version { get; }
    public ushort PageFlags { get; }
    public string SystemFilename { get; }
    public string ShellFilename { get; }
    public string Debugger1Filename { get; }
    public string Debugger2Filename { get; }
    public string StartupScreenName { get; }
    public string StartupProgramName { get; }
    public string ScrapFilename { get; }
    public ushort InitialFCBCount { get; }
    public ushort MaxEventQueueElements { get; }
    public uint SystemHeapSize { get; }
    public uint SystemHeapSize256K { get; }
    public uint SystemHeapSizeAll { get; }
    public ushort Filler { get; }
    public uint AdditionalSystemHeapSpace { get; }
    public uint FractionOfAvailableRAMForSystemHeap { get; }

    public HFSBootBlockHeader(Span<byte> data)
    {
        if (data.Length < 141)
        {
            throw new ArgumentException($"Boot block data must be at least {Size} bytes long.", nameof(data));
        }

        int offset = 0;

        // The boot block signature
        Signature = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;
        if (Signature != 0x4C4B) // 'LK' in big-endian
        {
            throw new InvalidDataException("Invalid DSK signature.");
        }

        // Boot code entry point
        EntryPoint = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Boot blocks version number
        Version = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        // Page flags (used internally)
        PageFlags = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        // System filename ASCII string
        SystemFilename = SpanUtilities.ReadFixedLengthStringWithLength(data, offset, 15);
        offset += 16;

        // Shell or Finder filename
        // ASCII string typically "Finder"
        ShellFilename = SpanUtilities.ReadFixedLengthStringWithLength(data, offset, 15);
        offset += 16;

        // Debugger 1 filename
        // ASCII string typically "Macsbug"
        Debugger1Filename = SpanUtilities.ReadFixedLengthStringWithLength(data, offset, 15);
        offset += 16;

        // Debugger 2 filename
        // ASCII string typically "Disassembler"
        Debugger2Filename = SpanUtilities.ReadFixedLengthStringWithLength(data, offset, 15);
        offset += 16;

        // The name of the startup screen
        // ASCII string typically "StartUpScreen"
        StartupScreenName = SpanUtilities.ReadFixedLengthStringWithLength(data, offset, 15);
        offset += 16;

        // The name of the startup program
        // ASCII string typically "Finder"
        StartupProgramName = SpanUtilities.ReadFixedLengthStringWithLength(data, offset, 15);
        offset += 16;

        // The scrap filename
        // ASCII string typically "Clipboard"
        ScrapFilename = SpanUtilities.ReadFixedLengthStringWithLength(data, offset, 15);
        offset += 16;

        // The (initial) number of allocated file control blocks (FCBs)
        InitialFCBCount = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        // The maximum number of event queue elements
        // This number determines the maximum number of events that the Event
        // Manager can store at any one time.
        // Usually this field contains the value 20.
        MaxEventQueueElements = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        // The system heap size on 128K Mac
        // The size of the System heap on a Macintosh computer having 128 KiB of RAM.
        SystemHeapSize = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // The system heap size on 256K Mac
        // The size of the System heap on a Macintosh computer having 256 KiB of RAM.
        SystemHeapSize256K = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // The system heap size on all machines
        // The size of the System heap on a Macintosh computer having 512 KiB or more of RAM.
        SystemHeapSizeAll = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Filler (used internally)
        Filler = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset));
        offset += 2;

        // Additional system heap space
        AdditionalSystemHeapSpace = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Fraction of available RAM for the system heap
        FractionOfAvailableRAMForSystemHeap = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        Debug.Assert(offset == Size);
    }
}
