using System.Diagnostics;
using System.Security.Cryptography;

namespace HfsReader.Tests;

public class HfsDiskTests
{
    #region Disk Loading Tests

    [Theory]
    [InlineData("hfs.dsk")]
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
    public void Ctor_Hfs()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "hfs.dsk"));
        var disk = new HFSDisk(stream);
        var volume = disk.Volumes[0];
        foreach (var file in volume.ContentsOfDirectory((HFSDirectory)volume.RootContents().First()))
        {
            if (!file.Name.Contains("Read Me"))
            {
                continue;
            }

            var data = volume.GetFileData(file as HFSFile ?? throw new InvalidOperationException(), HFSForkType.ResourceFork);
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
                volume.GetFileData(file, outputStream, HFSForkType.DataFork);
            }

            if (file.FileRecord.ResourceForkSize != 0)
            {
                using var outputStream = File.Create(filePath + ".res");
                volume.GetFileData(file, outputStream, HFSForkType.ResourceFork);
            }
        }
    }

    #endregion

    #region Disk Structure and Checksum Tests

    [Fact]
    public void Dropstuff40_VerifyStructureAndChecksums()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "dropstuff_40.dsk"));
        var disk = new HFSDisk(stream);
        var volume = disk.Volumes[0];

        // Verify counts
        Assert.Equal(2, CountFiles(volume));
        Assert.Equal(1, CountDirectories(volume));

        // Verify directory structure
        var directories = GetAllDirectories(volume);
        Assert.Contains("dropstuff_40", directories);

        // Verify files and checksums
        var files = GetAllFilesWithPaths(volume);

        // dropstuff_40/Desktop - Resource Fork (2880 bytes)
        var desktop = files.First(f => f.Path == "dropstuff_40/Desktop");
        Assert.Equal(2880u, desktop.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, desktop.File, "320F2714C172C41274AD9D0882708D28CDCFADF05293CC6ACC09826830D72FB9");

        // dropstuff_40/DropStuff w/EE™ 4.0 Installer - Data Fork (406087 bytes)
        var installer = files.First(f => f.Path.Contains("DropStuff"));
        Assert.Equal(406087u, installer.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, installer.File, "79590E772129AB38197B50D026546B1918B9BB6FFACDCF90F0F8B3CBF84B8772");
        Assert.Equal(95415u, installer.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, installer.File, "E903C9B19ED81257447965DAFD4A46C30FABCCB60D5856FF0B4B43D12CB6CD98");
    }

    [Fact]
    public void Exportfl131_VerifyStructureAndChecksums()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "exportfl-1.3.1.dsk"));
        var disk = new HFSDisk(stream);
        var volume = disk.Volumes[0];

        // Verify counts
        Assert.Equal(22, CountFiles(volume));
        Assert.Equal(2, CountDirectories(volume));

        // Verify directory structure
        var directories = GetAllDirectories(volume);
        Assert.Contains("ExportFl", directories);
        Assert.Contains("ExportFl/source", directories);

        // Verify key files and checksums
        var files = GetAllFilesWithPaths(volume);

        // ExportFl/COPYING.txt - Data Fork
        var copying = files.First(f => f.Path == "ExportFl/COPYING.txt");
        Assert.Equal(18007u, copying.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, copying.File, "FD86DC2D2A44BEF41FFD135F6BE6B5C3CDB0DF7E9805CE0792D07356823EB038");
        Assert.Equal(286u, copying.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, copying.File, "AC7410E3319FBF2D0C5F2BFCBE44BA0BC99D5A7DD94C5F4D07A3B34096715C18");

        // ExportFl/README.txt - Data Fork
        var readme = files.First(f => f.Path == "ExportFl/README.txt");
        Assert.Equal(662u, readme.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, readme.File, "EEB27914695E46C4CD6DB6EA74650905D160647712F92D351098779B6BA068E0");

        // ExportFl/ExportFl - Resource Fork
        var exportFl = files.First(f => f.Path == "ExportFl/ExportFl");
        Assert.Equal(13642u, exportFl.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, exportFl.File, "56F059909A3F12FB367DC97A44DE9ADF23E315FC7A0D36CB4CC9B9D56793D49D");

        // ExportFl/source/app.c - Data Fork
        var appC = files.First(f => f.Path == "ExportFl/source/app.c");
        Assert.Equal(954u, appC.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, appC.File, "C4EE8A210145C5EBD2B123C39C7B1C3610E878C765306BDFD0CB591904F18ECE");

        // ExportFl/source/MYMACAPI.i - Data Fork (largest source file)
        var myMacApi = files.First(f => f.Path == "ExportFl/source/MYMACAPI.i");
        Assert.Equal(83928u, myMacApi.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, myMacApi.File, "98A9993F240C3D53DF7E1E243EF398D7E9DDE553EE733FA76FA3D29D67572D60");
    }

    [Fact]
    public void StuffitExpander55_VerifyStructureAndChecksums()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "Stuffit_Expander_5.5.dsk"));
        var disk = new HFSDisk(stream);
        var volume = disk.Volumes[0];

        // Verify counts
        Assert.Equal(5, CountFiles(volume));
        Assert.Equal(2, CountDirectories(volume));

        // Verify directory structure
        var directories = GetAllDirectories(volume);
        Assert.Contains("Stuffit Expander 5.5", directories);
        Assert.Contains("Stuffit Expander 5.5/TheVolumeSettingsFolder", directories);

        // Verify files and checksums
        var files = GetAllFilesWithPaths(volume);

        // Stuffit Expander 5.5/Aladdin Expander™ 5.5 Installer
        var installer = files.First(f => f.Path.Contains("Aladdin Expander"));
        Assert.Equal(505470u, installer.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, installer.File, "831CAD72D6393FAE2848500EB5CBD2D918DA3580F521498684B7FAB076708019");
        Assert.Equal(319095u, installer.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, installer.File, "971E0FDA24F56789713A9ACA10066BA8819B5D06D4238BD97C8449F9584DED0E");

        // Stuffit Expander 5.5/Desktop
        var desktop = files.First(f => f.Path == "Stuffit Expander 5.5/Desktop");
        Assert.Equal(32308u, desktop.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, desktop.File, "867C77342D457C0BBFF22FB1BB791B9303FBC511685E2590143EB53A925D9BB6");

        // Stuffit Expander 5.5/SERIAL
        var serial = files.First(f => f.Path == "Stuffit Expander 5.5/SERIAL");
        Assert.Equal(51u, serial.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, serial.File, "40B5695FF9C51C2D05F02B3952DB6531BB7A37ED773EA789B403139456D6F2FE");
        Assert.Equal(332u, serial.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, serial.File, "26375D465C8C9C950C0DA1F03BBDB7EE4362497FF6A6A3BE3DD83C21C71CCE3F");
    }

    [Fact]
    public void Excel15_VerifyStructureAndChecksums()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "Excel 1.5/Excel 1.5.img"));
        var disk = new HFSDisk(stream);
        var volume = disk.Volumes[0];

        // Verify counts
        Assert.Equal(33, CountFiles(volume));
        Assert.Equal(2, CountDirectories(volume));

        // Verify directory structure
        var directories = GetAllDirectories(volume);
        Assert.Contains("Excel 1.5 Folder", directories);
        Assert.Contains("Excel 1.5 Folder/Sampler Files", directories);

        // Verify files and checksums
        var files = GetAllFilesWithPaths(volume);

        // Excel 1.5 Folder/Desktop - Resource Fork
        var desktop = files.First(f => f.Path == "Excel 1.5 Folder/Desktop");
        Assert.Equal(321u, desktop.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, desktop.File, "45764ECD672889770C22FAAFCE909F068135F959B6313B4270A0A35E165240CF");

        // Excel 1.5 Folder/Dialog Editor - Resource Fork
        var dialogEditor = files.First(f => f.Path == "Excel 1.5 Folder/Dialog Editor");
        Assert.Equal(25780u, dialogEditor.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, dialogEditor.File, "09AA0896DAF5C26515A556DE45B1F5018C6A32B334EBE1B535C478523E09F20E");

        // Excel 1.5 Folder/Microsoft Excel - Resource Fork (main application)
        var excel = files.First(f => f.Path == "Excel 1.5 Folder/Microsoft Excel");
        Assert.Equal(461000u, excel.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, excel.File, "C37199213F4CD21EAA3D804E23574881BB8B50884EDEF48EE661E758DA8D87C6");

        // Excel 1.5 Folder/Excel.Help - Resource Fork
        var help = files.First(f => f.Path == "Excel 1.5 Folder/Excel.Help");
        Assert.Equal(84742u, help.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, help.File, "C0782260423653CF2B0C101F7C15FC389063109FF065CFE1CCF111D0D1A0BA6B");

        // Excel 1.5 Folder/Expenses - Data Fork
        var expenses = files.First(f => f.Path == "Excel 1.5 Folder/Expenses");
        Assert.Equal(15333u, expenses.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, expenses.File, "08BFB3DEA5522A8F3C1B9BDCD517CFE73E01C49A18FA2C7EF6F747E86EB536CE");

        // Excel 1.5 Folder/Sampler Files/ACCOUNTS DATA - Data Fork
        var accountsData = files.First(f => f.Path == "Excel 1.5 Folder/Sampler Files/ACCOUNTS DATA");
        Assert.Equal(14293u, accountsData.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, accountsData.File, "BADFE0DDFDA10D13873A2CC46F3E9759199FE144882A0DDD8861951C49BA817A");

        // Excel 1.5 Folder/Sampler Files/BREAKEVEN - Data Fork
        var breakeven = files.First(f => f.Path == "Excel 1.5 Folder/Sampler Files/BREAKEVEN");
        Assert.Equal(6249u, breakeven.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, breakeven.File, "ABD973E4A191E0C6DA1649873B92C0D92305A7BB2C9D07BAF50DFE6E2EF47228");

        // Excel 1.5 Folder/Sampler Files/INVOICE TEMPLATE - Data Fork
        var invoiceTemplate = files.First(f => f.Path == "Excel 1.5 Folder/Sampler Files/INVOICE TEMPLATE");
        Assert.Equal(8189u, invoiceTemplate.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, invoiceTemplate.File, "4C5AEEA8DD4DD7C064F79D33E86A6AF52BA81A29B68EE98CE1B5FFB8AA950848");
    }

    [Fact]
    public void Excel30_VerifyStructureAndChecksums()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "Excel 3.0/Excel 3.0.img"));
        var disk = new HFSDisk(stream);
        var volume = disk.Volumes[0];

        // Verify counts
        Assert.Equal(59, CountFiles(volume));
        Assert.Equal(4, CountDirectories(volume));

        // Verify directory structure
        var directories = GetAllDirectories(volume);
        Assert.Contains("Excel 3.0 Folder", directories);
        Assert.Contains("Excel 3.0 Folder/Macro Library", directories);
        Assert.Contains("Excel 3.0 Folder/Macro Library/Custom Color Palettes", directories);
        Assert.Contains("Excel 3.0 Folder/Solver Examples", directories);

        // Verify files and checksums
        var files = GetAllFilesWithPaths(volume);

        // Excel 3.0 Folder/Desktop DB - Data Fork
        var desktopDb = files.First(f => f.Path == "Excel 3.0 Folder/Desktop DB");
        Assert.Equal(6144u, desktopDb.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, desktopDb.File, "DC2F6C4B2914F0B837C1E03AAF816364EA54FD5A45D310A0411DA1D567752357");

        // Excel 3.0 Folder/Excel Help - Data Fork
        var excelHelp = files.First(f => f.Path == "Excel 3.0 Folder/Excel Help");
        Assert.Equal(521612u, excelHelp.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, excelHelp.File, "6A09093D817FD9A3B79B837AE01335B2ABF4E032562F9C30140F2249C8DD445F");

        // Excel 3.0 Folder/Microsoft Excel - Resource Fork (main application)
        var excel = files.First(f => f.Path == "Excel 3.0 Folder/Microsoft Excel");
        Assert.Equal(1284441u, excel.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, excel.File, "FFF2C9E89FE91A0F69BB8111E7D03B0B284DF74D2A966CEBC108764CA894732D");

        // Excel 3.0 Folder/Microsoft Excel Tutorial - Data Fork
        var tutorial = files.First(f => f.Path == "Excel 3.0 Folder/Microsoft Excel Tutorial");
        Assert.Equal(767296u, tutorial.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, tutorial.File, "CEA03BB043131522517402784ACE6224317E6C9986C1F4DE4F99D2EB2A52EC22");
        Assert.Equal(23955u, tutorial.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, tutorial.File, "76F88E57E0E8B30DB7E26D4061ECA8542555573FB2D87159C05DF949B92334B4");

        // Excel 3.0 Folder/READ ME - Data Fork
        var readMe = files.First(f => f.Path == "Excel 3.0 Folder/READ ME");
        Assert.Equal(3013u, readMe.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, readMe.File, "1319A63EEE906A505447958504CF243FC39F2969F5881A1F0F2D7AB4E90D7B19");

        // Excel 3.0 Folder/Macro Library/Crosstabs - Data Fork
        var crosstabs = files.First(f => f.Path == "Excel 3.0 Folder/Macro Library/Crosstabs");
        Assert.Equal(56156u, crosstabs.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, crosstabs.File, "1954CFDEE21BFC05BEDAD30F7C0F58354402949C6B04C319A8BFF9D8C88ED1A1");

        // Excel 3.0 Folder/Macro Library/Sound Notes - Data Fork
        var soundNotes = files.First(f => f.Path == "Excel 3.0 Folder/Macro Library/Sound Notes");
        Assert.Equal(82643u, soundNotes.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, soundNotes.File, "59A49A9E1F4E46928030ED83D27AC2C9CD7A74A7B0D60EA286771614C125302C");

        // Excel 3.0 Folder/Macro Library/Worksheet Auditor - Data Fork
        var worksheetAuditor = files.First(f => f.Path == "Excel 3.0 Folder/Macro Library/Worksheet Auditor");
        Assert.Equal(60410u, worksheetAuditor.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, worksheetAuditor.File, "579001F15AE3CF07701A82AC7D65DA4ED8CCE8954530407724AF44CD18D8085D");

        // Excel 3.0 Folder/Macro Library/Custom Color Palettes/Rainbow - Data Fork
        var rainbow = files.First(f => f.Path == "Excel 3.0 Folder/Macro Library/Custom Color Palettes/Rainbow");
        Assert.Equal(1836u, rainbow.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, rainbow.File, "C4B60D862EADC8F224D094DB43E85A572D4FCC63A3449E1B321A5CCBC66C2D53");

        // Excel 3.0 Folder/Solver Examples/Sample - Data Fork
        var sample = files.First(f => f.Path == "Excel 3.0 Folder/Solver Examples/Sample");
        Assert.Equal(5851u, sample.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, sample.File, "2BE7D1F7CDFE1CE04E0F2CAEF690CFF312BEE9B416011724379AC4EE072A37C9");
    }

    [Fact]
    public void Excel50Disk2_VerifyStructureAndChecksums()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "Excel 5.0/Excel v5.0 (Disk 2) Install 2 (065-096-694).dsk"));
        var disk = new HFSDisk(stream);
        var volume = disk.Volumes[0];

        // Verify counts
        Assert.Equal(2, CountFiles(volume));
        Assert.Equal(1, CountDirectories(volume));

        // Verify directory structure
        var directories = GetAllDirectories(volume);
        Assert.Contains("Install Disk 2", directories);

        // Verify files and checksums
        var files = GetAllFilesWithPaths(volume);

        // Install Disk 2/Desktop - Resource Fork
        var desktop = files.First(f => f.Path == "Install Disk 2/Desktop");
        Assert.Equal(345u, desktop.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, desktop.File, "94D79A8A2798977041CD23D541C5C87B308C53F433C20EEA1211908DB3153C9A");

        // Install Disk 2/InstallPak - Data Fork
        var installPak = files.First(f => f.Path == "Install Disk 2/InstallPak");
        Assert.Equal(1431690u, installPak.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, installPak.File, "CEE65213CFBF87F85BAD6240FB10913523FC00A20B890AF1A19E9A95B0A1FA50");
        Assert.Equal(894u, installPak.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, installPak.File, "07D2568984745A8701B31310703BA14A54956B0492DBCB118C36A8D835B079F2");
    }

    [Fact]
    public void Excel50Disk3_VerifyStructureAndChecksums()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "Excel 5.0/Excel v5.0 (Disk 3) Install 3 (065-096-695).dsk"));
        var disk = new HFSDisk(stream);
        var volume = disk.Volumes[0];

        // Verify counts
        Assert.Equal(2, CountFiles(volume));
        Assert.Equal(1, CountDirectories(volume));

        // Verify directory structure
        var directories = GetAllDirectories(volume);
        Assert.Contains("Install Disk 3", directories);

        // Verify files and checksums
        var files = GetAllFilesWithPaths(volume);

        // Install Disk 3/InstallPak - Data Fork
        var installPak = files.First(f => f.Path == "Install Disk 3/InstallPak");
        Assert.Equal(1431690u, installPak.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, installPak.File, "37F6B1B4920B7E9738095260FA04E7772D693DA1AE632880065E5C3009A976FA");
        Assert.Equal(516u, installPak.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, installPak.File, "A58890393440311E2C4F528AAFBED02F873589DF3320E884EAAFE4DF38428D3F");
    }

    [Fact]
    public void Excel50Disk4_VerifyStructureAndChecksums()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "Excel 5.0/Excel v5.0 (Disk 4) Install 4 (065-096-696).dsk"));
        var disk = new HFSDisk(stream);
        var volume = disk.Volumes[0];

        // Verify counts
        Assert.Equal(2, CountFiles(volume));
        Assert.Equal(1, CountDirectories(volume));

        // Verify files and checksums
        var files = GetAllFilesWithPaths(volume);

        // Install Disk 4/InstallPak - Data Fork
        var installPak = files.First(f => f.Path == "Install Disk 4/InstallPak");
        Assert.Equal(1431690u, installPak.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, installPak.File, "299D942B53AAE1AAEA88B0E181694A94243291DF2F9DE4BDB5371E17D624DC2C");
        Assert.Equal(600u, installPak.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, installPak.File, "450FF672DC4A1E73697717348D61F08A544A855AC3A441BB2FC4560F7570C739");
    }

    [Fact]
    public void Excel50Disk5_VerifyStructureAndChecksums()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "Excel 5.0/Excel v5.0 (Disk 5) Install 5 (065-096-697).dsk"));
        var disk = new HFSDisk(stream);
        var volume = disk.Volumes[0];

        // Verify counts
        Assert.Equal(2, CountFiles(volume));
        Assert.Equal(1, CountDirectories(volume));

        // Verify files and checksums
        var files = GetAllFilesWithPaths(volume);

        // Install Disk 5/InstallPak - Data Fork
        var installPak = files.First(f => f.Path == "Install Disk 5/InstallPak");
        Assert.Equal(1431690u, installPak.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, installPak.File, "5646DCBBC90542567B33192235B2FF0829D6211A4AACD96E7027E6D6B6126ABA");
        Assert.Equal(1314u, installPak.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, installPak.File, "EFBB9A4B2356BEDC3BAF1AE2628406056808E56DAAAE80B368B13FBB9A439519");
    }

    [Fact]
    public void Excel50Disk11_VerifyStructureAndChecksums()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "Excel 5.0/Excel v5.0 (Disk 11) Install 11 (065-096-762).dsk"));
        var disk = new HFSDisk(stream);
        var volume = disk.Volumes[0];

        // Verify counts
        Assert.Equal(2, CountFiles(volume));
        Assert.Equal(1, CountDirectories(volume));

        // Verify files and checksums
        var files = GetAllFilesWithPaths(volume);

        // Install Disk 11/InstallPak - Data Fork
        var installPak = files.First(f => f.Path == "Install Disk 11/InstallPak");
        Assert.Equal(1431690u, installPak.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, installPak.File, "5699EE676BCDDD1A6375D8A0C2BE48FBBF5BAB0B9A677A1B2B5B59F6631065C3");
        Assert.Equal(516u, installPak.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, installPak.File, "2190E106EB58FCB32D99942EAE722A0D887D2E004FBA85609D41D468B10A753F");
    }

    [Fact]
    public void Excel50Disk12_VerifyStructureAndChecksums()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "Excel 5.0/Excel v5.0 (Disk 12) Install 12 (065-096-763).dsk"));
        var disk = new HFSDisk(stream);
        var volume = disk.Volumes[0];

        // Verify counts
        Assert.Equal(2, CountFiles(volume));
        Assert.Equal(1, CountDirectories(volume));

        // Verify files and checksums
        var files = GetAllFilesWithPaths(volume);

        // Install Disk 12/InstallPak - Data Fork
        var installPak = files.First(f => f.Path == "Install Disk 12/InstallPak");
        Assert.Equal(1431690u, installPak.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, installPak.File, "A131208605F83A5305E0EB20D05E09B31DFA7D2E282DAF50C438DC2DE94FFE5E");
        Assert.Equal(852u, installPak.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, installPak.File, "47B28E005C3727923136594F0C277E70D9B77DFF0C7CEC9750989F625C508FBB");
    }

    [Fact]
    public void Excel50Disk13_VerifyStructureAndChecksums()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "Excel 5.0/Excel v5.0 (Disk 13) Install 13 (065-096-764).dsk"));
        var disk = new HFSDisk(stream);
        var volume = disk.Volumes[0];

        // Verify counts
        Assert.Equal(2, CountFiles(volume));
        Assert.Equal(1, CountDirectories(volume));

        // Verify files and checksums
        var files = GetAllFilesWithPaths(volume);

        // Install Disk 13/InstallPak - Data Fork (smaller than other disks)
        var installPak = files.First(f => f.Path == "Install Disk 13/InstallPak");
        Assert.Equal(290190u, installPak.File.FileRecord.DataForkSize);
        AssertDataForkChecksum(volume, installPak.File, "A9CB529D87394ECE667BC5EDC9BD87F58B0B161187C7B561BA4B6D8D92748DC1");
        Assert.Equal(516u, installPak.File.FileRecord.ResourceForkSize);
        AssertResourceForkChecksum(volume, installPak.File, "CFB36B36D632156D74F7E1E16BA10459C0E884A6970FD9A440B0C2FB5930F378");
    }

    [Fact]
    public void System753_VerifyStructureAndChecksums()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "System753.dsk"));
        var disk = new HFSDisk(stream);
        var volume = disk.Volumes[0];

        // System 7.5.3 disk with full system folder
        Assert.Equal(184, CountFiles(volume));
        Assert.Equal(24, CountDirectories(volume));
    }

    [Fact]
    public void EmptyDisk100MB_VerifyStructureAndChecksums()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "100MB.dsk"));
        var disk = new HFSDisk(stream);
        var volume = disk.Volumes[0];

        // Contains just the root folder with Desktop file
        Assert.Equal(1, CountFiles(volume));
        Assert.Equal(1, CountDirectories(volume));
    }

    #endregion

    #region Helper Methods

    private static int CountFiles(HFSVolume volume)
    {
        return CountFilesRecursive(volume, volume.RootContents());
    }

    private static int CountFilesRecursive(HFSVolume volume, IEnumerable<HFSNode> nodes)
    {
        int count = 0;
        foreach (var node in nodes)
        {
            if (node is HFSFile)
            {
                count++;
            }
            else if (node is HFSDirectory directory)
            {
                count += CountFilesRecursive(volume, volume.ContentsOfDirectory(directory));
            }
        }
        return count;
    }

    private static int CountDirectories(HFSVolume volume)
    {
        return CountDirectoriesRecursive(volume, volume.RootContents());
    }

    private static int CountDirectoriesRecursive(HFSVolume volume, IEnumerable<HFSNode> nodes)
    {
        int count = 0;
        foreach (var node in nodes)
        {
            if (node is HFSDirectory directory)
            {
                count++;
                count += CountDirectoriesRecursive(volume, volume.ContentsOfDirectory(directory));
            }
        }
        return count;
    }

    private static List<string> GetAllDirectories(HFSVolume volume)
    {
        var directories = new List<string>();
        CollectDirectoriesRecursive(volume, volume.RootContents(), "", directories);
        return directories;
    }

    private static void CollectDirectoriesRecursive(HFSVolume volume, IEnumerable<HFSNode> nodes, string basePath, List<string> directories)
    {
        foreach (var node in nodes)
        {
            if (node is HFSDirectory directory)
            {
                var path = string.IsNullOrEmpty(basePath) ? node.Name : $"{basePath}/{node.Name}";
                directories.Add(path);
                CollectDirectoriesRecursive(volume, volume.ContentsOfDirectory(directory), path, directories);
            }
        }
    }

    private static List<(string Path, HFSFile File)> GetAllFilesWithPaths(HFSVolume volume)
    {
        var files = new List<(string Path, HFSFile File)>();
        CollectFilesRecursive(volume, volume.RootContents(), "", files);
        return files;
    }

    private static void CollectFilesRecursive(HFSVolume volume, IEnumerable<HFSNode> nodes, string basePath, List<(string Path, HFSFile File)> files)
    {
        foreach (var node in nodes)
        {
            var path = string.IsNullOrEmpty(basePath) ? node.Name : $"{basePath}/{node.Name}";
            if (node is HFSFile file)
            {
                files.Add((path, file));
            }
            else if (node is HFSDirectory directory)
            {
                CollectFilesRecursive(volume, volume.ContentsOfDirectory(directory), path, files);
            }
        }
    }

    private static void AssertDataForkChecksum(HFSVolume volume, HFSFile file, string expectedChecksum)
    {
        var data = volume.GetFileData(file, HFSForkType.DataFork);
        var actualChecksum = Convert.ToHexString(SHA256.HashData(data));
        Assert.Equal(expectedChecksum, actualChecksum);
    }

    private static void AssertResourceForkChecksum(HFSVolume volume, HFSFile file, string expectedChecksum)
    {
        var data = volume.GetFileData(file, HFSForkType.ResourceFork);
        var actualChecksum = Convert.ToHexString(SHA256.HashData(data));
        Assert.Equal(expectedChecksum, actualChecksum);
    }

    #endregion
}
