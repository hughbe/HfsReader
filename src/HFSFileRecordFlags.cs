namespace HfsReader;

[Flags]
public enum HFSFileRecordFlags : byte
{
    // 0x0001
    // File is locked and cannot be written to
    Locked = 0x0001,

    // 0x0002
    // Has thread record
    HasThreadRecord = 0x0002,

    // 0x0080
    // kHFSHasDateAddedMask
    // Had added date and time
    HasDateAdded = 0x0080
}
