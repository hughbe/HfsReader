namespace HfsReader;

public enum HFSFinderFlags : ushort
{
    // 0x0001
    // Is on desk
    // (used for files and folders)
    IsOnDesk = 0x0001,

    // 0x000e
    // Color
    // (used for files and folders)
    Color = 0x000E,

    // 0x0040
    // Is shared
    // if clear, the application needs to write to its resource fork, and therefore
    // cannot be shared on a server
    // (used for files)
    IsShared = 0x0040,

    // 0x0080
    // Has no inits
    // (used for files)
    HasNoInits = 0x0080,

    // Has been inited
    // Clear if the file contains desktop database resources that have not been added yet.
    // (used for files)
    HasBeenInited = 0x0100,

    // 0x0400
    // Has custom icon
    // (used for files and folders)
    HasCustomIcon = 0x0400,

    // 0x0800
    // Is stationary
    // (used for files)
    IsStationary = 0x0800,

    // 0x1000
    // Name locked
    // (used for files and folders)
    NameLocked = 0x1000,

    // 0x2000
    // Has bundle
    // (used for files)
    HasBundle = 0x2000,

    // 0x4000
    // Is invisible
    // (used for files and folders)
    IsInvisible = 0x4000,

    // 0x8000

    // Is alias
    // (used for files)
    IsAlias = 0x8000,
}
