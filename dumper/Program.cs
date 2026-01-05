using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using HfsReader;

public sealed class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp<ExtractCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("hfs-dumper");
            config.ValidateExamples();
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
        var volume = new HFSVolume(stream, settings.VolumeOffset);

        await ExtractDirectoryAsync(volume, volume.RootContents(), outputDir, settings, cancellationToken);

        AnsiConsole.MarkupLine($"[green]Extraction complete[/]: {outputDir.FullName}");
        return 0;
    }

    private async Task ExtractDirectoryAsync(HFSVolume volume, IEnumerable<HFSNode> nodes, DirectoryInfo outputDir, ExtractSettings settings, CancellationToken cancellationToken)
    {
        var entries = nodes.ToList();
        AnsiConsole.MarkupLine($"[green]Found[/] {entries.Count} items in directory.");

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var safeName = SanitizeName(entry.Name);

            if (entry is HFSDirectory directory)
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
            else if (entry is HFSFile file)
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
                    var bytes = volume.GetFileData(file, outputStream, HFSForkType.DataFork);
                    AnsiConsole.MarkupLine($"Wrote data fork: {Path.GetFileName(dataPath)} ({bytes} bytes)");
                    TrySetTimestamps(dataPath, file.FileRecord.CreationDate, file.FileRecord.ModificationDate);
                }

                if (extractResource)
                {
                    var resPath = basePath + ".res";
                    await using var outputStream = File.Create(resPath);
                    var bytes = volume.GetFileData(file, outputStream, HFSForkType.ResourceFork);
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
