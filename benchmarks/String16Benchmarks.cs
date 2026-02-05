using System.Text;
using BenchmarkDotNet.Attributes;
using HfsReader.Utilities;

namespace HfsReader.Benchmarks;

/// <summary>
/// Benchmarks comparing String16 (zero-allocation) vs string allocation for Pascal strings.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class String16Benchmarks
{
    private byte[] _sampleData = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create sample Pascal string data (first byte is length, followed by ASCII chars)
        // Simulates typical filenames like "System", "Finder", "Clipboard"
        _sampleData = new byte[16];
        string testString = "TestFilename";
        _sampleData[0] = (byte)testString.Length;
        Encoding.ASCII.GetBytes(testString, _sampleData.AsSpan(1));
    }

    /// <summary>
    /// Simulates the old allocating approach (same logic as SpanUtilities.ReadPascalString).
    /// </summary>
    private static string ReadPascalStringAllocating(ReadOnlySpan<byte> data)
    {
        var actualLength = data[0];
        return Encoding.ASCII.GetString(data.Slice(1, Math.Min(actualLength, data.Length - 1)));
    }

    [Benchmark(Baseline = true)]
    public string ReadPascalString_Allocating()
    {
        return ReadPascalStringAllocating(_sampleData);
    }

    [Benchmark]
    public String16 ReadPascalString_String16()
    {
        return new String16(_sampleData);
    }

    [Benchmark]
    public int String16_GetLength()
    {
        var str = new String16(_sampleData);
        return str.Length;
    }

    [Benchmark]
    public bool String16_Equals_Span()
    {
        var str = new String16(_sampleData);
        return str.Equals("TestFilename".AsSpan());
    }

    [Benchmark]
    public string String16_ToString()
    {
        var str = new String16(_sampleData);
        return str.ToString();
    }

    [Benchmark]
    public bool String16_TryFormat()
    {
        var str = new String16(_sampleData);
        Span<char> buffer = stackalloc char[16];
        return str.TryFormat(buffer, out _);
    }
}
