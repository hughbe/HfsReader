# HfsReader

HfsReader is a .NET library for reading classic Macintosh HFS disk images and extracting their contents. It provides a simple API to enumerate volumes, list directory contents, and read file forks (data and resource forks) from HFS volumes embedded in disk images.

---

## Features

- Detect Apple partition maps and locate HFS partitions inside disk images.
- Read master directory block and initialize the HFS catalog B-Tree.
- Enumerate directory contents (files and folders) from the catalog.
- Read file data and resource forks with correct extent handling (first 3 extents supported).
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
var disk = new HFSDisk(stream);
var volume = disk.Volumes[0]; // Pick the first HFS volume

// List root contents
foreach (var node in volume.RootContents())
{
    Console.WriteLine($"{(node is HFSDirectory ? "DIR" : "FILE")} {node.Name} (parent: {node.ParentIdentifier})");

    if (node is HFSFile file)
    {
        // Read the data fork
        byte[] data = volume.GetFileData(file, resourceFork: false);
        Console.WriteLine($"  Data fork size: {data.Length} bytes");
    }
}
```

---

## API Overview

### HFSDisk

- `HFSDisk(Stream stream)`: Constructs an HFSDisk by scanning the provided seekable, readable stream for Apple partition map entries. If no partition map is found, the stream is treated as a single HFS volume.
- `Volumes`: `List<HFSVolume>` — the list of detected HFS volumes in the disk image.

### HFSVolume

- `HFSVolume(Stream stream, int volumeStartOffset)`: Initialize an HFS volume reader given a stream and the byte offset where the volume begins.
- `BootBlock`: `HFSBootBlockHeader` — (internal structure; may be available depending on build).
- `MasterDirectoryBlock`: `HFSMasterDirectoryBlock` — contains allocation block size, catalog extents, and other volume metadata.
- `CatalogTree`: `BTree` — low-level access to the catalog B-Tree.
- `IEnumerable<HFSNode> RootContents()`: Enumerate top-level entries in the root directory.
- `IEnumerable<HFSNode> ContentsOfDirectory(HFSDirectory directory)`: Enumerate entries in a given directory.
- `byte[] GetFileData(HFSFile file, bool resourceFork)`: Read a file fork into a byte array.
- `int GetFileData(HFSFile file, Stream outputStream, bool resourceFork)`: Write a file fork to a stream and return the number of bytes written.

### HFSNode / HFSFile / HFSDirectory

- `HFSNode`: Base class with `ParentIdentifier` and `Name` properties and an abstract `Identifier`.
- `HFSFile`: Represents a file; exposes `HFSFileRecord FileRecord` for low-level metadata.
- `HFSDirectory`: Represents a folder; exposes `HFSFolderRecord FolderRecord`.

---

## Advanced Usage

### Reading a resource fork to a file

```csharp
using var outStream = File.Create("resource-fork.bin");
int written = volume.GetFileData(file, outStream, resourceFork: true);
Console.WriteLine($"Wrote {written} bytes");
```

### Inspecting the catalog B-Tree

Directly access `HFSVolume.CatalogTree` to traverse B-Tree nodes, examine `BTNode` descriptors, or dump catalog records for analysis.

---

## HFS Structure Notes

- The library reads the master directory block to determine allocation block size and catalog extents.
- File forks are stored in allocation blocks described by extent records. Currently the implementation reads the first three extents stored in the file record; extents overflow handling (Extents Overflow file) is not implemented and will throw if a file requires more than three extents.
- Catalog records are parsed from the Catalog B-Tree and presented as `HFSFile` and `HFSDirectory` nodes with their names and metadata.

---

## License

MIT License.
