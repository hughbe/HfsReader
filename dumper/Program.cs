using System.Buffers.Binary;
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using HfsReader;

public sealed class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.SetApplicationName("hfs-dumper");
            config.ValidateExamples();
            
            config.AddCommand<ExtractCommand>("extract")
                .WithDescription("Extract files from an HFS volume.");
            config.AddCommand<DumpTreeCommand>("dump-tree")
                .WithDescription("Pretty-print the B-tree structure of an HFS volume.");
        });

        return app.Run(args);
    }
}

sealed class ExtractSettings : CommandSettings
{
    [CommandArgument(0, "<input>")]
    public required string Input { get; init; }

    [CommandOption("-o|--output")]
    public string? Output { get; init; }

    [CommandOption("--data-only")]
    public bool DataOnly { get; init; }

    [CommandOption("--resource-only")]
    public bool ResourceOnly { get; init; }

    [CommandOption("--offset")]
    [Description("The byte offset within the input file where the HFS volume starts (default: 0).")]
    public int VolumeOffset { get; init; } = 0;
}

sealed class ExtractCommand : AsyncCommand<ExtractSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ExtractSettings settings, CancellationToken cancellationToken)
    {
        var input = new FileInfo(settings.Input);
        if (!input.Exists)
        {
            AnsiConsole.MarkupLine($"[red]Input file not found[/]: {input.FullName}");
            return -1;
        }

        var outputPath = settings.Output ?? Path.GetFileNameWithoutExtension(input.Name);
        var outputDir = new DirectoryInfo(outputPath);
        if (!outputDir.Exists)
        {
            outputDir.Create();
        }

        await using var stream = input.OpenRead();
        var volume = new HfsVolume(stream, settings.VolumeOffset);

        await ExtractDirectoryAsync(volume, volume.RootContents(), outputDir, settings, cancellationToken);

        AnsiConsole.MarkupLine($"[green]Extraction complete[/]: {outputDir.FullName}");
        return 0;
    }

    private async Task ExtractDirectoryAsync(HfsVolume volume, IEnumerable<HfsNode> nodes, DirectoryInfo outputDir, ExtractSettings settings, CancellationToken cancellationToken)
    {
        var entries = nodes.ToList();
        AnsiConsole.MarkupLine($"[green]Found[/] {entries.Count} items in directory.");

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var safeName = SanitizeName(entry.Name);

            if (entry is HfsDirectory directory)
            {
                var subDir = new DirectoryInfo(Path.Combine(outputDir.FullName, safeName));
                if (!subDir.Exists)
                {
                    subDir.Create();
                }

                AnsiConsole.MarkupLine($"[blue]Entering directory[/]: {safeName}");
                await ExtractDirectoryAsync(volume, volume.ContentsOfDirectory(directory), subDir, settings, cancellationToken);

                TrySetTimestamps(subDir.FullName, directory.FolderRecord.CreationDate, directory.FolderRecord.ContentModificationDate);
            }
            else if (entry is HfsFile file)
            {
                var basePath = Path.Combine(outputDir.FullName, safeName);

                bool extractData = !settings.ResourceOnly && file.FileRecord.DataForkSize != 0;
                bool extractResource = !settings.DataOnly && file.FileRecord.ResourceForkSize != 0;

                if (!extractData && !extractResource)
                {
                    AnsiConsole.MarkupLine($"[yellow]Skipping[/] {entry.Name} (no selected forks).");
                    continue;
                }

                if (extractData)
                {
                    var dataPath = basePath + ".data";
                    await using var outputStream = File.Create(dataPath);
                    var bytes = volume.GetFileData(file, outputStream, HfsForkType.DataFork);
                    AnsiConsole.MarkupLine($"Wrote data fork: {Path.GetFileName(dataPath)} ({bytes} bytes)");
                    TrySetTimestamps(dataPath, file.FileRecord.CreationDate, file.FileRecord.ModificationDate);
                }

                if (extractResource)
                {
                    var resPath = basePath + ".res";
                    await using var outputStream = File.Create(resPath);
                    var bytes = volume.GetFileData(file, outputStream, HfsForkType.ResourceFork);
                    AnsiConsole.MarkupLine($"Wrote resource fork: {Path.GetFileName(resPath)} ({bytes} bytes)");
                    TrySetTimestamps(resPath, file.FileRecord.CreationDate, file.FileRecord.ModificationDate);
                }
            }
        }
    }

    private static void TrySetTimestamps(string path, DateTime creationDate, DateTime modificationDate)
    {
        try
        {
            File.SetLastWriteTime(path, modificationDate);
            File.SetCreationTime(path, creationDate);
        }
        catch
        {
            // Ignore timestamp errors
        }
    }

    private static string SanitizeName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var invalidChar in invalidChars)
        {
            name = name.Replace(invalidChar, '_');
        }

        return name;
    }
}

sealed class DumpTreeSettings : CommandSettings
{
    [CommandArgument(0, "<input>")]
    [Description("The path to the HFS disk image file.")]
    public required string Input { get; init; }

    [CommandOption("-t|--tree")]
    [Description("The tree to dump: 'catalog' (default), 'extents', or 'all'.")]
    [DefaultValue("catalog")]
    public string Tree { get; init; } = "catalog";

    [CommandOption("--offset")]
    [Description("The byte offset within the input file where the HFS volume starts (default: 0).")]
    public int VolumeOffset { get; init; } = 0;

    [CommandOption("--max-depth")]
    [Description("Maximum depth to display in the tree (default: unlimited).")]
    public int? MaxDepth { get; init; }
}

sealed class DumpTreeCommand : AsyncCommand<DumpTreeSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, DumpTreeSettings settings, CancellationToken cancellationToken)
    {
        var input = new FileInfo(settings.Input);
        if (!input.Exists)
        {
            AnsiConsole.MarkupLine($"[red]Input file not found[/]: {input.FullName}");
            return Task.FromResult(-1);
        }

        using var stream = input.OpenRead();
        var volume = new HfsVolume(stream, settings.VolumeOffset);

        var treeName = settings.Tree.ToLowerInvariant();
        
        if (treeName == "catalog" || treeName == "all")
        {
            DumpCatalogTree(volume, settings.MaxDepth);
        }

        if (treeName == "extents" || treeName == "all")
        {
            DumpExtentsTree(volume, settings.MaxDepth);
        }

        if (treeName != "catalog" && treeName != "extents" && treeName != "all")
        {
            AnsiConsole.MarkupLine($"[red]Unknown tree type[/]: {settings.Tree}");
            AnsiConsole.MarkupLine("Valid options: 'catalog', 'extents', or 'all'.");
            return Task.FromResult(-1);
        }

        return Task.FromResult(0);
    }

    private static void DumpCatalogTree(HfsVolume volume, int? maxDepth)
    {
        AnsiConsole.WriteLine();
        
        var headerTable = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Catalog B-Tree Header[/]");
        
        headerTable.AddColumn("Property");
        headerTable.AddColumn("Value");
        
        var header = volume.CatalogTree.Header;
        headerTable.AddRow("Tree Depth", header.TreeDepth.ToString());
        headerTable.AddRow("Root Node Number", header.RootNodeNumber.ToString());
        headerTable.AddRow("Number of Data Records", header.NumberOfDataRecords.ToString());
        headerTable.AddRow("First Leaf Node", header.FirstLeafNodeNumber.ToString());
        headerTable.AddRow("Last Leaf Node", header.LastLeafNodeNumber.ToString());
        headerTable.AddRow("Node Size", $"{header.NodeSize} bytes");
        headerTable.AddRow("Maximum Key Size", $"{header.MaximumKeySize} bytes");
        headerTable.AddRow("Total Nodes", header.NumberOfNodes.ToString());
        headerTable.AddRow("Free Nodes", header.NumberOfFreeNodes.ToString());
        
        AnsiConsole.Write(headerTable);
        AnsiConsole.WriteLine();

        var tree = new Tree("[bold green]Catalog B-Tree Structure[/]");
        try
        {
            var rootNode = volume.CatalogTree.RootNode;
            AddCatalogNodeToTree(volume, tree, rootNode, 0, maxDepth);
        }
        catch (Exception ex)
        {
            tree.AddNode($"[red]Error reading tree: {EscapeMarkup(ex.Message)}[/]");
        }
        
        AnsiConsole.Write(tree);
    }

    private static void AddCatalogNodeToTree(HfsVolume volume, IHasTreeNodes parent, BTNode node, int currentDepth, int? maxDepth)
    {
        var nodeTypeColor = node.Descriptor.NodeType switch
        {
            BTNodeType.LeafNode => "green",
            BTNodeType.IndexNode => "yellow",
            BTNodeType.HeaderNode => "blue",
            BTNodeType.MapNode => "magenta",
            _ => "white"
        };

        var nodeLabel = $"[{nodeTypeColor}]Node {node.NodeIndex}[/] [dim](Level {node.Descriptor.NodeLevel}, {node.Descriptor.NodeType}, {node.Descriptor.RecordCount} records)[/]";
        var nodeTree = parent.AddNode(nodeLabel);

        if (maxDepth.HasValue && currentDepth >= maxDepth.Value)
        {
            if (node.Descriptor.RecordCount > 0)
            {
                nodeTree.AddNode("[dim]... (max depth reached)[/]");
            }
            return;
        }

        if (node.Descriptor.NodeType == BTNodeType.IndexNode)
        {
            for (int i = 0; i < node.Descriptor.RecordCount; i++)
            {
                try
                {
                    var recordOffset = node.RecordOffsets[i];
                    var indexKey = new HfsCatalogKey(volume.CatalogTree.BlockBuffer.Slice(recordOffset.Offset, recordOffset.Size), out int bytesRead);

                    // Skip empty/uninitialized records
                    if (indexKey.KeySize == 0)
                    {
                        continue;
                    }

                    var childNodeIndex = BinaryPrimitives.ReadUInt32BigEndian(volume.CatalogTree.BlockBuffer[(recordOffset.Offset + bytesRead)..]);

                    var keyLabel = $"[cyan]Key:[/] ParentID={indexKey.ParentIdentifier}, Name=\"{EscapeMarkup(indexKey.Name ?? "")}\" → [bold]Node {childNodeIndex}[/]";
                    var keyNode = nodeTree.AddNode(keyLabel);

                    var childNode = volume.CatalogTree.GetNode(childNodeIndex);
                    AddCatalogNodeToTree(volume, keyNode, childNode, currentDepth + 1, maxDepth);
                }
                catch (Exception ex)
                {
                    nodeTree.AddNode($"[red]Error reading record {i}: {EscapeMarkup(ex.Message)}[/]");
                }
            }
        }
        else if (node.Descriptor.NodeType == BTNodeType.LeafNode)
        {
            for (int i = 0; i < node.Descriptor.RecordCount; i++)
            {
                try
                {
                    var recordOffset = node.RecordOffsets[i];
                    var leafKey = new HfsCatalogKey(volume.CatalogTree.BlockBuffer.Slice(recordOffset.Offset, recordOffset.Size), out int bytesRead);

                    // Skip empty/uninitialized records
                    if (leafKey.KeySize == 0)
                    {
                        continue;
                    }

                    var dataOffset = recordOffset.Offset + bytesRead;
                    if ((dataOffset % 2) != 0)
                    {
                        dataOffset += 1;
                    }

                    var recordType = (HfsCatalogDataRecordType)BinaryPrimitives.ReadUInt16BigEndian(volume.CatalogTree.BlockBuffer[dataOffset..]);
                    var typeIcon = recordType switch
                    {
                        HfsCatalogDataRecordType.Folder => "📁",
                        HfsCatalogDataRecordType.File => "📄",
                        HfsCatalogDataRecordType.FolderThread => "🔗",
                        HfsCatalogDataRecordType.FileThread => "🔗",
                        _ => "❓"
                    };

                    var recordLabel = $"{typeIcon} [cyan]ParentID={leafKey.ParentIdentifier}[/] \"{EscapeMarkup(leafKey.Name ?? "")}\" [dim]({recordType})[/]";
                    nodeTree.AddNode(recordLabel);
                }
                catch (Exception ex)
                {
                    nodeTree.AddNode($"[red]Error reading record {i}: {EscapeMarkup(ex.Message)}[/]");
                }
            }

            if (node.Descriptor.NextNodeNumber != 0)
            {
                nodeTree.AddNode($"[dim]→ Next Leaf: Node {node.Descriptor.NextNodeNumber}[/]");
            }
        }
    }

    private static void DumpExtentsTree(HfsVolume volume, int? maxDepth)
    {
        AnsiConsole.WriteLine();

        if (volume.ExtentsOverflowTree == null)
        {
            AnsiConsole.MarkupLine("[yellow]No Extents Overflow B-Tree present in this volume.[/]");
            return;
        }

        var headerTable = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Extents Overflow B-Tree Header[/]");
        
        headerTable.AddColumn("Property");
        headerTable.AddColumn("Value");
        
        var header = volume.ExtentsOverflowTree.Header;
        headerTable.AddRow("Tree Depth", header.TreeDepth.ToString());
        headerTable.AddRow("Root Node Number", header.RootNodeNumber.ToString());
        headerTable.AddRow("Number of Data Records", header.NumberOfDataRecords.ToString());
        headerTable.AddRow("First Leaf Node", header.FirstLeafNodeNumber.ToString());
        headerTable.AddRow("Last Leaf Node", header.LastLeafNodeNumber.ToString());
        headerTable.AddRow("Node Size", $"{header.NodeSize} bytes");
        headerTable.AddRow("Maximum Key Size", $"{header.MaximumKeySize} bytes");
        headerTable.AddRow("Total Nodes", header.NumberOfNodes.ToString());
        headerTable.AddRow("Free Nodes", header.NumberOfFreeNodes.ToString());
        
        AnsiConsole.Write(headerTable);
        AnsiConsole.WriteLine();

        var tree = new Tree("[bold green]Extents Overflow B-Tree Structure[/]");
        try
        {
            var rootNode = volume.ExtentsOverflowTree.RootNode;
            AddExtentsNodeToTree(volume, tree, rootNode, 0, maxDepth);
        }
        catch (Exception ex)
        {
            tree.AddNode($"[red]Error reading tree: {EscapeMarkup(ex.Message)}[/]");
        }
        
        AnsiConsole.Write(tree);
    }

    private static void AddExtentsNodeToTree(HfsVolume volume, IHasTreeNodes parent, BTNode node, int currentDepth, int? maxDepth)
    {
        var nodeTypeColor = node.Descriptor.NodeType switch
        {
            BTNodeType.LeafNode => "green",
            BTNodeType.IndexNode => "yellow",
            BTNodeType.HeaderNode => "blue",
            BTNodeType.MapNode => "magenta",
            _ => "white"
        };

        var nodeLabel = $"[{nodeTypeColor}]Node {node.NodeIndex}[/] [dim](Level {node.Descriptor.NodeLevel}, {node.Descriptor.NodeType}, {node.Descriptor.RecordCount} records)[/]";
        var nodeTree = parent.AddNode(nodeLabel);

        if (maxDepth.HasValue && currentDepth >= maxDepth.Value)
        {
            if (node.Descriptor.RecordCount > 0)
            {
                nodeTree.AddNode("[dim]... (max depth reached)[/]");
            }
            return;
        }

        if (node.Descriptor.NodeType == BTNodeType.IndexNode)
        {
            for (int i = 0; i < node.Descriptor.RecordCount; i++)
            {
                try
                {
                    var recordOffset = node.RecordOffsets[i];
                    var indexKey = new HfsExtentsKey(volume.ExtentsOverflowTree!.BlockBuffer.Slice(recordOffset.Offset, HfsExtentsKey.Size));
                    var childNodeIndex = BinaryPrimitives.ReadUInt32BigEndian(volume.ExtentsOverflowTree.BlockBuffer[(recordOffset.Offset + HfsExtentsKey.Size)..]);

                    var forkLabel = indexKey.ForkType == HfsForkType.DataFork ? "Data" : "Resource";
                    var keyLabel = $"[cyan]Key:[/] FileID={indexKey.FileID}, Fork={forkLabel}, StartBlock={indexKey.StartBlock} → [bold]Node {childNodeIndex}[/]";
                    var keyNode = nodeTree.AddNode(keyLabel);

                    var childNode = volume.ExtentsOverflowTree.GetNode(childNodeIndex);
                    AddExtentsNodeToTree(volume, keyNode, childNode, currentDepth + 1, maxDepth);
                }
                catch (Exception ex)
                {
                    nodeTree.AddNode($"[red]Error reading record {i}: {EscapeMarkup(ex.Message)}[/]");
                }
            }
        }
        else if (node.Descriptor.NodeType == BTNodeType.LeafNode)
        {
            for (int i = 0; i < node.Descriptor.RecordCount; i++)
            {
                try
                {
                    var recordOffset = node.RecordOffsets[i];
                    var leafKey = new HfsExtentsKey(volume.ExtentsOverflowTree!.BlockBuffer.Slice(recordOffset.Offset, HfsExtentsKey.Size));

                    var forkIcon = leafKey.ForkType == HfsForkType.DataFork ? "💾" : "📦";
                    var forkLabel = leafKey.ForkType == HfsForkType.DataFork ? "Data" : "Resource";
                    var recordLabel = $"{forkIcon} [cyan]FileID={leafKey.FileID}[/] {forkLabel} Fork, StartBlock={leafKey.StartBlock}";
                    nodeTree.AddNode(recordLabel);
                }
                catch (Exception ex)
                {
                    nodeTree.AddNode($"[red]Error reading record {i}: {EscapeMarkup(ex.Message)}[/]");
                }
            }

            if (node.Descriptor.NextNodeNumber != 0)
            {
                nodeTree.AddNode($"[dim]→ Next Leaf: Node {node.Descriptor.NextNodeNumber}[/]");
            }
        }
    }

    private static string EscapeMarkup(string text)
    {
        return text.Replace("[", "[[").Replace("]", "]]");
    }
}
