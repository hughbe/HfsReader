using BenchmarkDotNet.Attributes;

namespace HfsReader.Benchmarks;

/// <summary>
/// Benchmarks for reading HFS disks, volumes, and enumerating entries.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class HfsDiskBenchmarks
{
    private byte[] _diskData = null!;
    private MemoryStream _diskStream = null!;
    private HfsDisk _disk = null!;
    private HfsVolume _volume = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Load a representative disk image into memory for consistent benchmarking
        // Try to use a disk with more content for better benchmarking
        string[] candidateDisks = [
            "Microsoft Excel 2.2 for Macintosh.dsk",
            "System753.dsk",
            "hfs.dsk"
        ];
        
        string? diskPath = null;
        foreach (var candidate in candidateDisks)
        {
            var path = Path.Combine("Samples", candidate);
            if (File.Exists(path))
            {
                diskPath = path;
                break;
            }
        }
        
        if (diskPath == null)
        {
            throw new FileNotFoundException("No sample disk found. Run from the benchmarks directory.");
        }
        
        _diskData = File.ReadAllBytes(diskPath);
        _diskStream = new MemoryStream(_diskData);
        _disk = new HfsDisk(_diskStream);
        _volume = _disk.Volumes[0];
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _diskStream?.Dispose();
    }

    [Benchmark]
    public HfsDisk LoadDisk()
    {
        // Benchmark loading a disk from a fresh stream
        using var stream = new MemoryStream(_diskData);
        return new HfsDisk(stream);
    }

    [Benchmark]
    public int EnumerateRootContents()
    {
        // Benchmark enumerating root directory contents
        int count = 0;
        foreach (var node in _volume.RootContents())
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    public int EnumerateRootContents_AccessNameString31()
    {
        // Benchmark enumerating and accessing NameString31 (zero allocation)
        int count = 0;
        foreach (var node in _volume.RootContents())
        {
            // Access NameString31 property - this is zero allocation
            var length = node.NameString31.Length;
            count += length > 0 ? 1 : 0;
        }
        return count;
    }

    [Benchmark]
    public int EnumerateRootContents_AccessNameString()
    {
        // Benchmark enumerating and accessing Name property (allocates string)
        int count = 0;
        foreach (var node in _volume.RootContents())
        {
            // Access Name property - this allocates a string
            var name = node.Name;
            count += name.Length > 0 ? 1 : 0;
        }
        return count;
    }

    [Benchmark]
    public List<HfsNode> MaterializeRootContents()
    {
        // Benchmark materializing root contents to a list (common operation)
        return _volume.RootContents().ToList();
    }

    [Benchmark]
    public bool FindFileByName_ZeroAlloc()
    {
        // Benchmark finding a file by name without allocating strings
        foreach (var node in _volume.RootContents())
        {
            // Use zero-allocation Equals
            if (node.NameString31.Equals("Desktop".AsSpan()) || 
                node.NameString31.Equals("System Folder".AsSpan()) ||
                node.NameString31.Equals("Microsoft Excel".AsSpan()))
            {
                return true;
            }
        }
        return false;
    }

    [Benchmark]
    public bool FindFileByName_Allocating()
    {
        // Benchmark finding a file by name with string allocation
        foreach (var node in _volume.RootContents())
        {
            // Uses Name property which allocates
            if (node.Name == "Desktop" || 
                node.Name == "System Folder" ||
                node.Name == "Microsoft Excel")
            {
                return true;
            }
        }
        return false;
    }
}
