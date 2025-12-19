using System.Diagnostics;

namespace HfsReader.Tests;

public class HfsDiskTests
{
    [Theory]
    [InlineData("Microsoft Excel 1.03.dsk")]
    //[InlineData("excel2.2.img")]
    [InlineData("Microsoft Excel 2.2 for Macintosh.dsk")]
    [InlineData("MS EXCEL SETUP.dsk")]
    [InlineData("MS EXCEL UTILITIES 1.dsk")]
    [InlineData("MS EXCEL UTILITIES 2.dsk")]
    [InlineData("100MB.dsk")]
    [InlineData("test.iso")]
    [InlineData("dropstuff_40.dsk")]
    [InlineData("System753.dsk")]
    [InlineData("importfl-1.2.2.dsk")]
    [InlineData("exportfl-1.3.1.dsk")]
    //[InlineData("ResEdit 2.1.3.img")]
    [InlineData("Stuffit_Expander_5.5.dsk")]
    //[InlineData("Excel 1.03/Excel Program.image")]
    [InlineData("Excel 1.5/Excel 1.5.img")]
    //[InlineData("Excel 2.2a/Help & Examples.image")]
    //[InlineData("Excel 2.2a/Microsoft Excel.image")]
    //[InlineData("Excel 2.2a/Tour.image")]
    [InlineData("Excel 3.0/Excel 3.0.img")]
    [InlineData("Excel 4.0/Excel 4.0.img")]
    [InlineData("Excel 5.0/Excel v5.0 (Disk 1) Install 1 (065-096-693).dsk")]
    [InlineData("Excel 5.0/Excel v5.0 (Disk 2) Install 2 (065-096-694).dsk")]
    [InlineData("Excel 5.0/Excel v5.0 (Disk 3) Install 3 (065-096-695).dsk")]
    [InlineData("Excel 5.0/Excel v5.0 (Disk 4) Install 4 (065-096-696).dsk")]
    [InlineData("Excel 5.0/Excel v5.0 (Disk 5) Install 5 (065-096-697).dsk")]
    [InlineData("Excel 5.0/Excel v5.0 (Disk 6) Install 6 (065-096-698).dsk")]
    [InlineData("Excel 5.0/Excel v5.0 (Disk 7) Install 7 (065-096-699).dsk")]
    [InlineData("Excel 5.0/Excel v5.0 (Disk 8) Install 8 (065-096-700).dsk")]
    [InlineData("Excel 5.0/Excel v5.0 (Disk 9) Install 9 (065-096-701).dsk")]
    [InlineData("Excel 5.0/Excel v5.0 (Disk 10) Install 10 (065-096-702).dsk")]
    [InlineData("Excel 5.0/Excel v5.0 (Disk 11) Install 11 (065-096-762).dsk")]
    [InlineData("Excel 5.0/Excel v5.0 (Disk 12) Install 12 (065-096-763).dsk")]
    [InlineData("Excel 5.0/Excel v5.0 (Disk 13) Install 13 (065-096-764).dsk")]
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
