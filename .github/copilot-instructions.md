# Project Guidelines

## Code Style

- **Target**: .NET 9.0 with nullable reference types and implicit usings enabled
- **Binary parsing**: Use `System.Buffers.Binary.BinaryPrimitives` for big-endian reads (HFS is big-endian)
- **Spans over arrays**: Prefer `ReadOnlySpan<byte>` for parsing; avoid allocations where possible
- **Struct constructors**: Binary structs take `ReadOnlySpan<byte> data` and parse inline with offset tracking (see [HfsMasterDirectoryBlock.cs](src/HfsMasterDirectoryBlock.cs), [HfsCatalogKey.cs](src/HfsCatalogKey.cs))
- **Fixed-size strings**: Use `String16`, `String27`, `String31` inline arrays for Pascal strings (see [Utilities/](src/Utilities/))
- **XML docs**: All public APIs require XML documentation comments

## Architecture

- **HfsDisk** → detects Apple partition maps, creates **HfsVolume** instances
- **HfsVolume** → reads master directory block, initializes catalog **BTree**
- **BTree<TKey, TComparison>** → generic B-tree with key interface `IBTKey<TKey, TComparison>`
- **HfsNode** → base for **HfsFile** and **HfsDirectory**
- Data flows: Stream → HfsDisk → HfsVolume → BTree → catalog records → file data via extent chains

## Build and Test

```sh
# Build entire solution
dotnet build HfsReader.sln

# Run tests
dotnet test tests/HfsReader.Tests.csproj

# Run benchmarks (Release mode)
dotnet run -c Release --project benchmarks/HfsReader.Benchmarks.csproj
```

## Project Conventions

- **Constructor parsing**: Structs parse binary data in constructor, throwing `InvalidDataException` for invalid data
- **Offset tracking**: Use `int offset = 0` and increment after each field read
- **Extent handling**: First 3 extents via `HfsExtentRecord`; extents overflow file for additional
- **Test samples**: `.dsk` and `.img` files in [tests/Samples/](tests/Samples/) - use `[Theory]` with `[InlineData]` for parameterized tests
- **InternalsVisibleTo**: Benchmarks project can access internal members

## Key Files

- [src/HfsMasterDirectoryBlock.cs](src/HfsMasterDirectoryBlock.cs) - exemplar binary parsing pattern
- [src/BTree.cs](src/BTree.cs) - generic B-tree implementation
- [src/Utilities/SpanUtilities.cs](src/Utilities/SpanUtilities.cs) - timestamp/string parsing helpers
- [tests/HfsDiskTests.cs](tests/HfsDiskTests.cs) - comprehensive test patterns

## Reference

- [HFS Format Specification](https://github.com/libyal/libfshfs/blob/main/documentation/Hierarchical%20File%20System%20(HFS).asciidoc) - detailed documentation of the HFS on-disk format
