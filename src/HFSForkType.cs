namespace HfsReader;

/// <summary>
/// Represents the fork type for an extent key.
/// </summary>
public enum HFSForkType : byte
{
    /// <summary>
    /// Data fork.
    /// </summary>
    DataFork = 0x00,

    /// <summary>
    /// Resource fork.
    /// </summary>
    ResourceFork = 0xFF
}
