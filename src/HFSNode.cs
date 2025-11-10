namespace HfsReader;

public abstract class HFSNode
{
    public uint ParentIdentifier { get; }
    public string Name { get; }
    public abstract uint Identifier { get; }

    public HFSNode(uint parentIdentifier, string name)
    {
        ParentIdentifier = parentIdentifier;
        Name = name;
    }
}
