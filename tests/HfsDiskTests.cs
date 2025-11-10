using System.Diagnostics;

namespace HfsReader.Tests;

public class HfsDiskTests
{
    [Theory]
    [InlineData("Microsoft Excel 1.03.dsk")]
    [InlineData("Microsoft Excel 2.2 for Macintosh.dsk")]
    [InlineData("MS EXCEL SETUP.dsk")]
    [InlineData("MS EXCEL UTILITIES 1.dsk")]
    [InlineData("MS EXCEL UTILITIES 2.dsk")]
    [InlineData("100MB.dsk")]
    [InlineData("test.iso")]
    public void Ctor_Stream(string diskName)
    {
        var filePath = Path.Combine("Samples", diskName);
        using var stream = File.OpenRead(filePath);
        var dsk = new HFSDisk(stream);

        foreach (var volume in dsk.Volumes)
        {
            var rootContents = volume.RootContents().ToList();
            Debug.WriteLine($"Found {rootContents.Count} items in root:");
            foreach (var rootNode in rootContents)
            {
                Debug.WriteLine($"- {rootNode.Name} (ID: {rootNode.Identifier}, Parent: {rootNode.ParentIdentifier})");
                PrintNode(volume, rootNode);
            }

            var outputPath = Path.Combine("Output", Path.GetFileNameWithoutExtension(diskName));
            foreach (var rootNode in rootContents)
            {
                ExportFile(volume, rootNode, outputPath);
            }
        }
    }

    [Fact]
    public void Ctor_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("stream", () => new HFSDisk(null!));
    }

    private void PrintNode(HFSVolume volume, HFSNode node, string indent = "")
    {
        if (node is HFSDirectory directory)
        {
            Debug.WriteLine($"{indent}- {node.Name} (ID: {node.Identifier})");
            foreach (var child in volume.ContentsOfDirectory(directory))
            {
                PrintNode(volume, child, indent + "  ");
            }
        }
        else if (node is HFSFile file)
        {
            Debug.WriteLine($"{indent}- {node.Name} (ID: {node.Identifier}, Data Size: {file.FileRecord.DataForkSize} bytes, Resource Size: {file.FileRecord.ResourceForkSize} bytes)");
        }
    }

    private void ExportFile(HFSVolume volume, HFSNode node, string path)
    {
        // Ensure the output directory exists
        Directory.CreateDirectory(path);
        
        if (node is HFSDirectory directory)
        {
            // Skip root directory (which has name "/") - just process its children
            if (directory.Name != "/")
            {
                // Sanitize directory names for filesystem compatibility
                var safeName = directory.Name.Replace("/", "_").Replace(":", "_");
                path = Path.Combine(path, safeName);
                Directory.CreateDirectory(path);
            }
            
            foreach (var child in volume.ContentsOfDirectory(directory))
            {
                ExportFile(volume, child, path);
            }
        }
        else if (node is HFSFile file)
        {
            // Sanitize file names for filesystem compatibility
            var safeName = file.Name.Replace("/", "_").Replace(":", "_");
            var filePath = Path.Combine(path, safeName);
            
            if (file.FileRecord.DataForkSize != 0)
            {
                using var outputStream = File.Create(filePath + ".data");
                volume.GetFileData(file, outputStream, false);
            }

            if (file.FileRecord.ResourceForkSize != 0)
            {
                using var outputStream = File.Create(filePath + ".res");
                volume.GetFileData(file, outputStream, true);
            }
        }
    }
}
