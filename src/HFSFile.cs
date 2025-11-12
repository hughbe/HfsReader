namespace HfsReader;

/// <summary>
/// Represents a file in an HFS volume.
/// </summary>
public class HFSFile : HFSNode
{
    /// <summary>
    /// Gets the unique identifier for this file.
    /// </summary>
    public override uint Identifier => FileRecord.Identifier;

    /// <summary>
    /// Gets the file record associated with this file.
    /// </summary>
    public HFSFileRecord FileRecord { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HFSFile"/> class.
    /// </summary>
    /// <param name="parentIdentifier">The identifier of the parent directory.</param>
    /// <param name="name">The name of the file.</param>
    /// <param name="fileRecord">The file record associated with this file.</param>
    public HFSFile(uint parentIdentifier, string name, HFSFileRecord fileRecord) : base(parentIdentifier, name)
    {
        FileRecord = fileRecord;
    }
}
