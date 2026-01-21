namespace HfsReader;

/// <summary>
/// Represents a comparison key for HFSExtentsKey.
/// </summary>
/// <param name="FileID">The file identifier.</param>
/// <param name="ForkType">The fork type.</param>
/// <param name="StartBlock">The starting allocation block number.</param>
public record HFSExtentsKeyComparison(uint FileID, HFSForkType ForkType, ushort StartBlock)
{
}
