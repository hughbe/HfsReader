using HfsReader.Utilities;

namespace HfsReader;

/// <summary>
/// Represents a file in an HFS volume.
/// </summary>
public class HfsFile : HfsNode
{
    /// <summary>
    /// Gets the unique identifier for this file.
    /// </summary>
    public override uint Identifier => FileRecord.Identifier;

    /// <summary>
    /// Gets the file record associated with this file.
    /// </summary>
    public HfsFileRecord FileRecord { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HfsFile"/> class.
    /// </summary>
    /// <param name="parentIdentifier">The identifier of the parent directory.</param>
    /// <param name="name">The name of the file.</param>
    /// <param name="fileRecord">The file record associated with this file.</param>
    public HfsFile(uint parentIdentifier, String31 name, HfsFileRecord fileRecord) : base(parentIdentifier, name)
    {
        FileRecord = fileRecord;
    }
}
