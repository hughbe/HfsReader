namespace HfsReader;

public class HFSFile : HFSNode
{
    public override uint Identifier => FileRecord.Identifier;

    public HFSFileRecord FileRecord { get; }

    public HFSFile(uint parentIdentifier, string name, HFSFileRecord fileRecord) : base(parentIdentifier, name)
    {
        FileRecord = fileRecord;
    }
}
