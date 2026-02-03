namespace HfsReader;

/// <summary>
/// Represents the fork type for an extent key.
/// </summary>
public enum HfsForkType
{
    /// <summary>
    /// Data fork.
    /// </summary>
    DataFork,

    /// <summary>
    /// Resource fork.
    /// </summary>
    ResourceFork = 0xFF
}
