namespace HfsReader;

/// <summary>
/// Represents the type of a B-tree node.
/// </summary>
public enum BTNodeKind : sbyte
{
    /// <summary>
    /// A leaf node containing data records.
    /// </summary>
    Leaf = -1,

    /// <summary>
    /// An index node containing pointers to other nodes.
    /// </summary>
    Index = 0,

    /// <summary>
    /// A header node containing B-tree metadata.
    /// </summary>
    Header = 1,

    /// <summary>
    /// A map node containing allocation information.
    /// </summary>
    Map = 2
}
