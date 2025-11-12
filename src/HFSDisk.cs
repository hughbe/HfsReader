using System.Buffers.Binary;

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

        Span<byte> blockBuffer = stackalloc byte[512];

        // See if this is an ISO 9660 image with an embedded HFS volume.
        // The primary volume descriptor starts at byte offset 32768.
        // Try to read Apple Partition Map entries first.
        int currentOffset = 512;
        while (stream.Seek(currentOffset, SeekOrigin.Begin) < stream.Length)
        {
            currentOffset += 512;

            // Read the signature first.
            if (stream.Read(blockBuffer[..2]) != 2)
            {
                throw new InvalidDataException("Unable to read Apple Partition Map Entry.");
            }

            var signature = BinaryPrimitives.ReadUInt16BigEndian(blockBuffer);
            if (signature != ApplePartitionMapEntry.BlockSignature)
            {
                break;
            }

            // Read the rest of the partition entry.
            if (stream.Read(blockBuffer[2..]) != blockBuffer.Length - 2)
            {
                throw new InvalidDataException("Unable to read Apple Partition Map Entry.");
            }

            var partitionEntry = new ApplePartitionMapEntry(blockBuffer);
            if (partitionEntry.Type == "Apple_HFS")
            {
                // Found the HFS partition - add a volume for it.
                var hfsStartOffset = (long)partitionEntry.PartitionStartBlock * 512;
                Volumes.Add(new HFSVolume(stream, (int)hfsStartOffset));
            }
        }

        // If no HFS volumes found, assume the entire image is a single HFS volume.
        if (Volumes.Count == 0)
        {
            Volumes.Add(new HFSVolume(stream, 0));
        }
    }
}
