using ApplePartitionMapReader;

namespace HfsReader;

/// <summary>
/// Represents a disk containing one or more HFS volumes.
/// </summary>
public class HFSDisk
{
    /// <summary>
    /// Gets the list of HFS volumes found on the disk.
    /// </summary>
    public List<HFSVolume> Volumes { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="HFSDisk"/> class and scans for HFS volumes.
    /// </summary>
    /// <param name="stream">The stream containing the disk image data.</param>
    public HFSDisk(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek || !stream.CanRead)
        {
            throw new ArgumentException("Stream must be seekable and readable.", nameof(stream));
        }

        // Try to read Apple Partition Map entries first.
        if (ApplePartitionMap.IsApplePartitionMap(stream, 0))
        {
            var partitionMap = new ApplePartitionMap(stream, 0);
            foreach (var partitionEntry in partitionMap.Entries)
            {
                if (partitionEntry.Type == ApplePartitionMapIdentifiers.AppleHFS)
                {
                    // Found the HFS partition - add a volume for it.
                    var hfsStartOffset = (long)partitionEntry.PartitionStartBlock * 512;
                    Volumes.Add(new HFSVolume(stream, (int)hfsStartOffset));
                }
            }
        }

        // If no HFS volumes found, assume the entire image is a single HFS volume.
        if (Volumes.Count == 0)
        {
            Volumes.Add(new HFSVolume(stream, 0));
        }
    }
}
