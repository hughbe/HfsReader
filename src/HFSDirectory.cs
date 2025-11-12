namespace HfsReader;

/// <summary>
/// Represents a directory in an HFS volume.
/// </summary>
public class HFSDirectory : HFSNode
{
    /// <summary>
    /// Gets the unique identifier for this directory.
    /// </summary>
    public override uint Identifier => FolderRecord.Identifier;

    /// <summary>
    /// Gets the folder record associated with this directory.
    /// </summary>
    public HFSFolderRecord FolderRecord { get; }

    internal HFSDirectory(uint parentIdentifier, string name, HFSFolderRecord folderRecord) : base(parentIdentifier, name)
    {
        FolderRecord = folderRecord;
    }
}