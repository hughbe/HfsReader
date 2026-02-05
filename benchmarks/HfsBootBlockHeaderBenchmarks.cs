using BenchmarkDotNet.Attributes;

namespace HfsReader.Benchmarks;

/// <summary>
/// Benchmarks for parsing HfsBootBlockHeader with String16 vs allocating strings.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class HfsBootBlockHeaderBenchmarks
{
    private byte[] _bootBlockData = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create valid boot block header data
        _bootBlockData = new byte[148];
        
        int offset = 0;
        
        // Signature: 'LK' = 0x4C4B
        _bootBlockData[offset++] = 0x4C;
        _bootBlockData[offset++] = 0x4B;
        
        // Entry point (4 bytes)
        offset += 4;
        
        // Version (2 bytes)
        offset += 2;
        
        // Page flags (2 bytes)
        offset += 2;
        
        // System filename (16 bytes Pascal string)
        WritePascalString(_bootBlockData.AsSpan(offset, 16), "System");
        offset += 16;
        
        // Shell filename (16 bytes)
        WritePascalString(_bootBlockData.AsSpan(offset, 16), "Finder");
        offset += 16;
        
        // Debugger1 filename (16 bytes)
        WritePascalString(_bootBlockData.AsSpan(offset, 16), "Macsbug");
        offset += 16;
        
        // Debugger2 filename (16 bytes)
        WritePascalString(_bootBlockData.AsSpan(offset, 16), "Disassembler");
        offset += 16;
        
        // Startup screen name (16 bytes)
        WritePascalString(_bootBlockData.AsSpan(offset, 16), "StartUpScreen");
        offset += 16;
        
        // Startup program name (16 bytes)
        WritePascalString(_bootBlockData.AsSpan(offset, 16), "Finder");
        offset += 16;
        
        // Scrap filename (16 bytes)
        WritePascalString(_bootBlockData.AsSpan(offset, 16), "Clipboard");
        offset += 16;
        
        // Rest is numeric fields, leave as zeros
    }

    private static void WritePascalString(Span<byte> destination, string value)
    {
        destination[0] = (byte)value.Length;
        System.Text.Encoding.ASCII.GetBytes(value, destination[1..]);
    }

    [Benchmark]
    public HfsBootBlockHeader ParseBootBlockHeader()
    {
        return new HfsBootBlockHeader(_bootBlockData);
    }

    [Benchmark]
    public int ParseAndAccessAllStrings()
    {
        var header = new HfsBootBlockHeader(_bootBlockData);
        
        // Access all string properties to simulate real usage
        int totalLength = 0;
        totalLength += header.SystemFilename.Length;
        totalLength += header.ShellFilename.Length;
        totalLength += header.Debugger1Filename.Length;
        totalLength += header.Debugger2Filename.Length;
        totalLength += header.StartupScreenName.Length;
        totalLength += header.StartupProgramName.Length;
        totalLength += header.ScrapFilename.Length;
        
        return totalLength;
    }

    [Benchmark]
    public bool ParseAndCompareStrings()
    {
        var header = new HfsBootBlockHeader(_bootBlockData);
        
        // Compare strings without allocation using Equals(ReadOnlySpan<char>)
        return header.SystemFilename.Equals("System".AsSpan()) &&
               header.ShellFilename.Equals("Finder".AsSpan());
    }
}
