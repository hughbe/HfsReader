namespace HfsReader;

/// <summary>
/// Represents flags for an HFS file record.
/// </summary>
[Flags]
public enum HFSFileRecordFlags : byte
{
    /// <summary>File is locked and cannot be written to.</summary>
    Locked = 0x0001,

    /// <summary>Has thread record.</summary>
    HasThreadRecord = 0x0002,

    /// <summary>Has added date and time.</summary>
    HasDateAdded = 0x0080
}
