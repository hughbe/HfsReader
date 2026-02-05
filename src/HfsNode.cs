using HfsReader.Utilities;

namespace HfsReader;

/// <summary>
/// Represents a node in the HFS file system hierarchy (file or directory).
/// </summary>
public abstract class HfsNode
{
    /// <summary>
    /// Gets the identifier of the parent node.
    /// </summary>
    public uint ParentIdentifier { get; }

    /// <summary>
    /// Gets the name of the node as a fixed-size string (zero-allocation access).
    /// </summary>
    public String31 NameString31 { get; }

    /// <summary>
    /// Gets the name of the node as a string.
    /// </summary>
    /// <remarks>
    /// This property allocates a new string on each access. For performance-critical
    /// code, use <see cref="NameString31"/> instead.
    /// </remarks>
    public string Name => NameString31.ToString();

    /// <summary>
    /// Gets the unique identifier for this node.
    /// </summary>
    public abstract uint Identifier { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HfsNode"/> class.
    /// </summary>
    /// <param name="parentIdentifier">The identifier of the parent node.</param>
    /// <param name="name">The name of the node.</param>
    public HfsNode(uint parentIdentifier, String31 name)
    {
        ParentIdentifier = parentIdentifier;
        NameString31 = name;
    }
}
