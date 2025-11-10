namespace HfsReader;

public enum HFSCatalogDataRecordType : ushort
{
    Folder = 0x0100,
    File = 0x0200,
    FolderThread = 0x0300,
    FileThread = 0x0400
}
