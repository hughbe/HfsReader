using System.Buffers.Binary;
using System.Diagnostics;
using HfsReader.Utilities;

namespace HfsReader;

/// <summary>
/// Represents the boot block header of an HFS volume.
/// </summary>
public struct HfsBootBlockHeader
{
    /// <summary>
    /// The size of the boot block header in bytes.
    /// </summary>
    public const int Size = 148;

    /// <summary>
    /// Gets the boot block signature.
    /// </summary>
    public ushort Signature { get; }

    /// <summary>
    /// Gets the boot code entry point.
    /// </summary>
    public uint EntryPoint { get; }

    /// <summary>
    /// Gets the boot blocks version number.
    /// </summary>
    public ushort Version { get; }

    /// <summary>
    /// Gets the page flags (used internally).
    /// </summary>
    public ushort PageFlags { get; }

    /// <summary>
    /// Gets the system filename.
    /// </summary>
    public String16 SystemFilename { get; }

    /// <summary>
    /// Gets the shell or Finder filename.
    /// </summary>
    public String16 ShellFilename { get; }

    /// <summary>
    /// Gets the Debugger 1 filename.
    /// </summary>
    public String16 Debugger1Filename { get; }

    /// <summary>
    /// Gets the Debugger 2 filename.
    /// </summary>
    public String16 Debugger2Filename { get; }

    /// <summary>
    /// Gets the name of the startup screen.
    /// </summary>
    public String16 StartupScreenName { get; }

    /// <summary>
    /// Gets the name of the startup program.
    /// </summary>
    public String16 StartupProgramName { get; }

    /// <summary>
    /// Gets the scrap filename.
    /// </summary>
    public String16 ScrapFilename { get; }

    /// <summary>
    /// Gets the initial number of allocated file control blocks (FCBs).
    /// </summary>
    public ushort InitialFCBCount { get; }

    /// <summary>
    /// Gets the maximum number of event queue elements.
    /// </summary>
    public ushort MaxEventQueueElements { get; }

    /// <summary>
    /// Gets the system heap size on 128K Mac.
    /// </summary>
    public uint SystemHeapSize128K { get; }

    /// <summary>
    /// Gets the system heap size on 256K Mac.
    /// </summary>
    public uint SystemHeapSize256K { get; }

    /// <summary>
    /// Gets the system heap size on all machines.
    /// </summary>
    public uint SystemHeapSizeAll { get; }

    /// <summary>
    /// Gets the filler (used internally).
    /// </summary>
    public ushort Filler { get; }

    /// <summary>
    /// Gets the additional system heap space.
    /// </summary>
    public uint AdditionalSystemHeapSpace { get; }

    /// <summary>
    /// Gets the fraction of available RAM for the system heap.
    /// </summary>
    public uint FractionOfAvailableRAMForSystemHeap { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HfsBootBlockHeader"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the boot block header data.</param>
    public HfsBootBlockHeader(Span<byte> data)
    {
        if (data.Length < 141)
        {
            throw new ArgumentException($"Boot block data must be at least {Size} bytes long.", nameof(data));
        }

        int offset = 0;

        // The boot block signature
        Signature = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        if (Signature != 0x4C4B) // 'LK' in big-endian
        {
            throw new InvalidDataException("Invalid DSK signature.");
        }

        // Boot code entry point
        EntryPoint = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Boot blocks version number
        Version = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // Page flags (used internally)
        PageFlags = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // System filename ASCII string
        SystemFilename = new String16(data.Slice(offset, 16));
        offset += 16;

        // Shell or Finder filename
        // ASCII string typically "Finder"
        ShellFilename = new String16(data.Slice(offset, 16));
        offset += 16;

        // Debugger 1 filename
        // ASCII string typically "Macsbug"
        Debugger1Filename = new String16(data.Slice(offset, 16));
        offset += 16;

        // Debugger 2 filename
        // ASCII string typically "Disassembler"
        Debugger2Filename = new String16(data.Slice(offset, 16));
        offset += 16;

        // The name of the startup screen
        // ASCII string typically "StartUpScreen"
        StartupScreenName = new String16(data.Slice(offset, 16));
        offset += 16;

        // The name of the startup program
        // ASCII string typically "Finder"
        StartupProgramName = new String16(data.Slice(offset, 16));
        offset += 16;

        // The scrap filename
        // ASCII string typically "Clipboard"
        ScrapFilename = new String16(data.Slice(offset, 16));
        offset += 16;

        // The (initial) number of allocated file control blocks (FCBs)
        InitialFCBCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // The maximum number of event queue elements
        // This number determines the maximum number of events that the Event
        // Manager can store at any one time.
        // Usually this field contains the value 20.
        MaxEventQueueElements = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // The system heap size on 128K Mac
        // The size of the System heap on a Macintosh computer having 128 KiB of RAM.
        SystemHeapSize128K = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // The system heap size on 256K Mac
        // The size of the System heap on a Macintosh computer having 256 KiB of RAM.
        SystemHeapSize256K = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // The system heap size on all machines
        // The size of the System heap on a Macintosh computer having 512 KiB or more of RAM.
        SystemHeapSizeAll = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Filler (used internally)
        Filler = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // Additional system heap space
        AdditionalSystemHeapSpace = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // Fraction of available RAM for the system heap
        FractionOfAvailableRAMForSystemHeap = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        Debug.Assert(offset == data.Length, "Did not read the expected number of bytes for HfsBootBlockHeader.");
    }
}
