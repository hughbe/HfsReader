namespace HfsReader;

public class HFSDirectory : HFSNode
{
    public override uint Identifier => FolderRecord.Identifier;

    public HFSFolderRecord FolderRecord { get; }

    internal HFSDirectory(uint parentIdentifier, string name, HFSFolderRecord folderRecord) : base(parentIdentifier, name)
    {
        FolderRecord = folderRecord;
    }
}