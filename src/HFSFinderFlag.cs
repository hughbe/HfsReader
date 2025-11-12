namespace HfsReader;

/// <summary>
/// Represents Finder flags for files and folders.
/// </summary>
public enum HFSFinderFlags : ushort
{
    /// <summary>Is on desk (used for files and folders).</summary>
    IsOnDesk = 0x0001,

    /// <summary>Color (used for files and folders).</summary>
    Color = 0x000E,

    /// <summary>Is shared - if clear, the application needs to write to its resource fork (used for files).</summary>
    IsShared = 0x0040,

    /// <summary>Has no inits (used for files).</summary>
    HasNoInits = 0x0080,

    /// <summary>Has been inited - clear if the file contains desktop database resources not yet added (used for files).</summary>
    HasBeenInited = 0x0100,

    /// <summary>Has custom icon (used for files and folders).</summary>
    HasCustomIcon = 0x0400,

    /// <summary>Is stationary (used for files).</summary>
    IsStationary = 0x0800,

    /// <summary>Name locked (used for files and folders).</summary>
    NameLocked = 0x1000,

    /// <summary>Has bundle (used for files).</summary>
    HasBundle = 0x2000,

    /// <summary>Is invisible (used for files and folders).</summary>
    IsInvisible = 0x4000,

    /// <summary>Is alias (used for files).</summary>
    IsAlias = 0x8000,
}
