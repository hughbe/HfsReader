namespace HfsReader;

/// <summary>
/// Represents a comparison key for HFSCatalogKey.
/// </summary>
/// <param name="ParentIdentifier">The parent identifier of the key.</param>
/// <param name="Name">The name associated with the key.</param>
public record HFSCatalogKeyComparison(uint ParentIdentifier, string? Name)
{
}
