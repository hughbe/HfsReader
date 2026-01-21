namespace HfsReader;

/// <summary>
/// A key within a binary tree.
/// </summary>
public interface IBTKey<TKey, TComparison> where TKey : IBTKey<TKey, TComparison>
{
    /// <summary>
    /// Creates the key from the given data.
    /// </summary>
    /// <param name="data">The data to create the key from.</param>
    /// <param name="bytesRead">The number of bytes read from the data.</param>
    /// <returns>The created key.</returns>
    static abstract TKey Create(ReadOnlySpan<byte> data, out int bytesRead);

    /// <summary>
    /// Compares this key to another key.
    /// </summary>
    /// <param name="other">The other key to compare to.</param>
    /// <returns>A value indicating the relative order of the keys.</returns>
    int CompareTo(TComparison other);

    /// <summary>
    /// Determines if this key is a parent of the given key.
    /// </summary>
    /// <param name="other">The other key to check.</param>
    /// <returns>True if this key is a parent of the other key; otherwise, false.</returns>
    bool IsParent(TComparison other);
}
