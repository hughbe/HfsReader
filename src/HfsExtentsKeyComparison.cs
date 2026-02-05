namespace HfsReader;

/// <summary>
/// Represents a comparison key for HfsExtentsKey.
/// </summary>
/// <param name="FileID">The file identifier.</param>
/// <param name="ForkType">The fork type.</param>
/// <param name="StartBlock">The starting allocation block number.</param>
public readonly record struct HfsExtentsKeyComparison(uint FileID, HfsForkType ForkType, ushort StartBlock)
{
}
