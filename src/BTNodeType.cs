namespace HfsReader;

/// <summary>
/// Represents the type of a B-tree node.
/// </summary>
public enum BTNodeType : sbyte
{
    /// <summary>
    /// A leaf node containing data records.
    /// </summary>
    LeafNode = -1,

    /// <summary>
    /// An index node containing pointers to other nodes.
    /// </summary>
    IndexNode = 0,

    /// <summary>
    /// A header node containing B-tree metadata.
    /// </summary>
    HeaderNode = 1,

    /// <summary>
    /// A map node containing allocation information.
    /// </summary>
    MapNode = 2
}
