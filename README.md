# HfsReader

HfsReader is a .NET library for reading classic Macintosh HFS disk images and extracting their contents. It provides a simple API to enumerate volumes, list directory contents, and read file forks (data and resource forks) from HFS volumes embedded in disk images.

---

## Features

- Detect Apple partition maps and locate HFS partitions inside disk images.
- Read master directory block and initialize the HFS catalog B-Tree.
- Enumerate directory contents (files and folders) from the catalog.
- Read file data and resource forks with full extent handling, including extents overflow support.
- Low-level access to B-Tree and catalog structures for advanced inspection.

---

## Installation

Add the project or reference the compiled library in your .NET application. If published on NuGet, you could install it like:

```sh
dotnet add package HfsReader
```

Or reference the project directly:

```sh
dotnet add reference ../HfsReader/HfsReader.csproj
```

---

## Quick Start Example

```csharp
using HfsReader;
using System.IO;

// Open a disk image (can be a .dsk file containing an HFS volume or an image with
// an Apple partition map).
using var stream = File.OpenRead("Samples/Microsoft Excel 1.03.dsk");

// Parse the disk and find HFS volumes.
var disk = new HfsDisk(stream);
var volume = disk.Volumes[0]; // Pick the first HFS volume

// List root contents
foreach (var node in volume.RootContents())
{
    Console.WriteLine($"{(node is HfsDirectory ? "DIR" : "FILE")} {node.Name} (parent: {node.ParentIdentifier})");

    if (node is HfsFile file)
    {
        // Read the data fork
        byte[] data = volume.GetDataForkData(file);
        Console.WriteLine($"  Data fork size: {data.Length} bytes");
    }
}
```

---

## API Overview

### HfsDisk

- `HfsDisk(Stream stream)`: Constructs an HfsDisk by scanning the provided seekable, readable stream for Apple partition map entries. If no partition map is found, the stream is treated as a single HFS volume.
- `Volumes`: `List<HfsVolume>` — the list of detected HFS volumes in the disk image.

### HfsVolume

- `HfsVolume(Stream stream, int volumeStartOffset)`: Initialize an HFS volume reader given a stream and the byte offset where the volume begins.
- `BootBlock`: `HfsBootBlockHeader` — boot block header metadata.
- `MasterDirectoryBlock`: `HfsMasterDirectoryBlock` — contains allocation block size, catalog extents, and other volume metadata.
- `CatalogTree`: `HfsBTree<HfsCatalogKey, HfsCatalogKeyComparison>` — low-level access to the catalog B-Tree.
- `ExtentsOverflowTree`: `HfsBTree<HfsExtentsKey, HfsExtentsKeyComparison>?` — low-level access to the extents overflow B-Tree (null if the volume has no overflow extents).
- `IEnumerable<HfsNode> RootContents()`: Enumerate top-level entries in the root directory.
- `IEnumerable<HfsNode> ContentsOfDirectory(HfsDirectory directory)`: Enumerate entries in a given directory.
- `byte[] GetDataForkData(HfsFile file)`: Read a file's data fork into a byte array.
- `int GetDataForkData(HfsFile file, Stream outputStream)`: Write a file's data fork to a stream and return the number of bytes written.
- `byte[] GetResourceForkData(HfsFile file)`: Read a file's resource fork into a byte array.
- `int GetResourceForkData(HfsFile file, Stream outputStream)`: Write a file's resource fork to a stream and return the number of bytes written.
- `byte[] GetFileData(HfsFile file, HfsForkType forkType)`: Read a file fork into a byte array.
- `int GetFileData(HfsFile file, Stream outputStream, HfsForkType forkType)`: Write a file fork to a stream and return the number of bytes written.

### HfsForkType

- `DataFork`: The data fork of a file.
- `ResourceFork`: The resource fork of a file.

### HfsNode / HfsFile / HfsDirectory

- `HfsNode`: Base class with `ParentIdentifier`, `Name`, and abstract `Identifier` properties.
- `HfsFile`: Represents a file; exposes `HfsFileRecord FileRecord` for low-level metadata.
- `HfsDirectory`: Represents a folder; exposes `HfsFolderRecord FolderRecord`.

---

## Advanced Usage

### Reading a resource fork to a file

```csharp
using var outStream = File.Create("resource-fork.bin");
int written = volume.GetResourceForkData(file, outStream);
Console.WriteLine($"Wrote {written} bytes");
```

### Inspecting the catalog B-Tree

Directly access `HfsVolume.CatalogTree` to traverse B-Tree nodes, examine `BTNode` descriptors, or dump catalog records for analysis.

---

## HFS Structure Notes

- The library reads the master directory block to determine allocation block size and catalog extents.
- File forks are stored in allocation blocks described by extent records. The first three extents are stored in the file record; additional extents are read from the Extents Overflow B-Tree.
- Catalog records are parsed from the Catalog B-Tree and presented as `HfsFile` and `HfsDirectory` nodes with their names and metadata.

---

## License

MIT License.

## Related Projects

- [AppleDiskImageReader](https://github.com/hughbe/AppleDiskImageReader) - Reader for Apple II universal disk (.2mg) images
- [AppleIIDiskReader](https://github.com/hughbe/AppleIIDiskReader) - Reader for Apple II DOS 3.3 disk (.dsk) images
- [ProDosVolumeReader](https://github.com/hughbe/ProDosVolumeReader) - Reader for ProDOS (.po) volumes
- [WozDiskImageReader](https://github.com/hughbe/WozDiskImageReader) - Reader for WOZ (.woz) disk images
- [DiskCopyReader](https://github.com/hughbe/DiskCopyReader) - Reader for Disk Copy 4.2 (.dc42) images
- [MfsReader](https://github.com/hughbe/MfsReader) - Reader for MFS (Macintosh File System) volumes
- [HfsPlusReader](https://github.com/hughbe/HfsPlusReader) - Reader for HFS+ (Hierarchical File System+) volumes
- [ApplePartitionMapReader](https://github.com/hughbe/ApplePartitionMapReader) - Reader for Apple Partition Map (APM) images
- [ResourceForkReader](https://github.com/hughbe/ResourceForkReader) - Reader for Macintosh resource forks
- [BinaryIIReader](https://github.com/hughbe/BinaryIIReader) - Reader for Binary II (.bny, .bxy) archives
- [StuffItReader](https://github.com/hughbe/StuffItReader) - Reader for StuffIt (.sit) archives
- [ShrinkItReader](https://github.com/hughbe/ShrinkItReader) - Reader for ShrinkIt (.shk, .sdk) archives
