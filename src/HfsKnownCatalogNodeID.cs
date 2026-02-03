namespace HfsReader;

/// <summary>
/// Represents well-known catalog node identifiers (CNIDs) in Hfs.
/// </summary>
public enum HfsKnownCatalogNodeID : uint
{
    /// <summary>
    /// Reserved.
    /// </summary>
    Reserved = 0,

    /// <summary>
    /// Parent identifier of the root directory (folder).
    /// </summary>
    kHfsRootParentID = 1,

    /// <summary>
    /// Directory identifier of the root directory (folder).
    /// </summary>
    kHfsRootFolderID = 2,

    /// <summary>
    /// The extents (overflow) file.
    /// </summary>
    kHfsExtentsFileID = 3,

    /// <summary>
    /// The catalog file.
    /// </summary>
    kHfsCatalogFileID = 4,

    /// <summary>
    /// The bad allocation block file.
    /// </summary>
    kHfsBadBlockFileID = 5,

    /// <summary>
    /// The allocation file (Hfs+).
    /// </summary>
    kHfsAllocationFileID = 6,

    /// <summary>
    /// The startup file (Hfs+).
    /// </summary>
    kHfsStartupFileID = 7,

    /// <summary>
    /// The attributes file (Hfs+).
    /// </summary>
    kHfsAttributesFileID = 8,

    /// <summary>
    /// Used temporarily by fsck_hfs when rebuilding the catalog file.
    /// </summary>
    kHfsRepairCatalogFileID = 14,

    /// <summary>
    /// The bogus extent file used temporarily during exchange files operations.
    /// </summary>
    kHfsBogusExtentFileID = 15,

    /// <summary>
    /// The first available CNID for user's files and folders.
    /// </summary>
    kHfsFirstUserCatalogNodeID = 16
}
