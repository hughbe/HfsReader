namespace HfsReader;

/// <summary>
/// Represents a node in the HFS file system hierarchy (file or directory).
/// </summary>
public abstract class HFSNode
{
    /// <summary>
    /// Gets the identifier of the parent node.
    /// </summary>
    public uint ParentIdentifier { get; }

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the unique identifier for this node.
    /// </summary>
    public abstract uint Identifier { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HFSNode"/> class.
    /// </summary>
    /// <param name="parentIdentifier">The identifier of the parent node.</param>
    /// <param name="name">The name of the node.</param>
    public HFSNode(uint parentIdentifier, string name)
    {
        ParentIdentifier = parentIdentifier;
        Name = name;
    }
}
