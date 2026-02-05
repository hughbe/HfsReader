using HfsReader.Utilities;

namespace HfsReader;

/// <summary>
/// Represents a directory in an HFS volume.
/// </summary>
public class HfsDirectory : HfsNode
{
    /// <summary>
    /// Gets the unique identifier for this directory.
    /// </summary>
    public override uint Identifier => FolderRecord.Identifier;

    /// <summary>
    /// Gets the folder record associated with this directory.
    /// </summary>
    public HfsFolderRecord FolderRecord { get; }

    internal HfsDirectory(uint parentIdentifier, String31 name, HfsFolderRecord folderRecord) : base(parentIdentifier, name)
    {
        FolderRecord = folderRecord;
    }
}