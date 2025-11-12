namespace HfsReader;

/// <summary>
/// Represents the type of an HFS catalog data record.
/// </summary>
public enum HFSCatalogDataRecordType : ushort
{
    /// <summary>A folder record.</summary>
    Folder = 0x0100,
    /// <summary>A file record.</summary>
    File = 0x0200,
    /// <summary>A folder thread record.</summary>
    FolderThread = 0x0300,
    /// <summary>A file thread record.</summary>
    FileThread = 0x0400
}
