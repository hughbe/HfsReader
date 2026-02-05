using System.Text;
using BenchmarkDotNet.Attributes;
using HfsReader.Utilities;

namespace HfsReader.Benchmarks;

/// <summary>
/// Benchmarks for HfsCatalogKey parsing and comparison operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class HfsCatalogKeyBenchmarks
{
    private byte[] _catalogKeyData = null!;
    private HfsCatalogKey _catalogKey;
    private HfsCatalogKeyComparison _comparisonKey;
    private String31 _string31Name;

    [GlobalSetup]
    public void Setup()
    {
        // Create a valid HfsCatalogKey data structure
        // Format: KeySize (1) + Reserved (1) + ParentID (4) + Name (Pascal string, 32 bytes max)
        _catalogKeyData = new byte[38]; // 1 + 37 max key size
        
        int offset = 0;
        
        // KeySize: 6 (reserved) + 4 (parent ID) + name length + 1 (length byte) = min 6, max 37
        string testName = "TestFileName.txt";
        int keySize = 1 + 4 + 1 + testName.Length; // reserved + parentID + lengthByte + name
        _catalogKeyData[offset++] = (byte)keySize;
        
        // Reserved
        _catalogKeyData[offset++] = 0;
        
        // ParentIdentifier (big-endian)
        _catalogKeyData[offset++] = 0x00;
        _catalogKeyData[offset++] = 0x00;
        _catalogKeyData[offset++] = 0x00;
        _catalogKeyData[offset++] = 0x02; // Parent ID = 2
        
        // Pascal string: length byte + string bytes
        _catalogKeyData[offset++] = (byte)testName.Length;
        Encoding.ASCII.GetBytes(testName, _catalogKeyData.AsSpan(offset));
        
        // Parse the key
        _catalogKey = new HfsCatalogKey(_catalogKeyData, out _);
        
        // Create comparison key for benchmarking CompareTo
        _comparisonKey = new HfsCatalogKeyComparison(2, "TestFileName.txt");
        
        // Create String31 by copying from the parsed key
        _string31Name = _catalogKey.Name;
    }

    [Benchmark]
    public HfsCatalogKey ParseCatalogKey()
    {
        return new HfsCatalogKey(_catalogKeyData, out _);
    }

    [Benchmark]
    public int CompareTo_ZeroAllocation()
    {
        // This uses the new zero-allocation CompareTo via ReadOnlySpan<char>
        return _catalogKey.CompareTo(_comparisonKey);
    }

    [Benchmark]
    public int String31_CompareTo_String31()
    {
        // Direct String31 to String31 comparison
        return _catalogKey.Name.CompareTo(_string31Name);
    }

    [Benchmark]
    public int String31_CompareTo_Span()
    {
        // String31 compared to span (zero-allocation)
        return _catalogKey.Name.CompareTo("TestFileName.txt".AsSpan());
    }

    [Benchmark]
    public int String31_CompareTo_Allocating()
    {
        // Old approach: implicit conversion to string, then compare (allocates)
        string name = _catalogKey.Name; // implicit conversion allocates
        return string.Compare(name, "TestFileName.txt", StringComparison.Ordinal);
    }

    [Benchmark]
    public bool String31_Equals_Span()
    {
        return _catalogKey.Name.Equals("TestFileName.txt".AsSpan());
    }

    [Benchmark]
    public bool String31_Equals_Allocating()
    {
        string name = _catalogKey.Name; // implicit conversion allocates
        return name == "TestFileName.txt";
    }

    [Benchmark]
    public int String31_GetLength()
    {
        return _catalogKey.Name.Length;
    }

    [Benchmark]
    public string String31_ToString()
    {
        // This always allocates - used when you actually need the string
        return _catalogKey.Name.ToString();
    }
}
