namespace HfsReader;

/// <summary>
/// Represents the offset and size of a record within a B-tree node.
/// </summary>
public struct BTRecordOffset
{
    /// <summary>
    /// Gets or sets the offset of the record from the start of the node.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// Gets or sets the size of the record in bytes.
    /// </summary>
    public int Size { get; set; }
}
