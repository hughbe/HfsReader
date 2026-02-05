namespace HfsReader;

/// <summary>
/// Represents the offset and size of a record within a B-tree node.
/// </summary>
public readonly struct BTRecordOffset
{
    /// <summary>
    /// Gets the offset of the record from the start of the node.
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    /// Gets the size of the record in bytes.
    /// </summary>
    public int Size { get; init; }
}
