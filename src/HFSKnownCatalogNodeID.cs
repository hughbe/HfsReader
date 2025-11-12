namespace HfsReader;

/// <summary>
/// Represents well-known catalog node identifiers (CNIDs) in HFS.
/// </summary>
public enum HFSKnownCatalogNodeID : uint
{
    /// <summary>Reserved.</summary>
    Reserved = 0,

    /// <summary>Parent identifier of the root directory (folder).</summary>
    kHFSRootParentID = 1,

    /// <summary>Directory identifier of the root directory (folder).</summary>
    kHFSRootFolderID = 2,

    /// <summary>The extents (overflow) file.</summary>
    kHFSExtentsFileID = 3,

    /// <summary>The catalog file.</summary>
    kHFSCatalogFileID = 4,

    /// <summary>The bad allocation block file.</summary>
    kHFSBadBlockFileID = 5,

    /// <summary>The allocation file (HFS+).</summary>
    kHFSAllocationFileID = 6,

    /// <summary>The startup file (HFS+).</summary>
    kHFSStartupFileID = 7,

    /// <summary>The attributes file (HFS+).</summary>
    kHFSAttributesFileID = 8,

    /// <summary>Used temporarily by fsck_hfs when rebuilding the catalog file.</summary>
    kHFSRepairCatalogFileID = 14,

    /// <summary>The bogus extent file used temporarily during exchange files operations.</summary>
    kHFSBogusExtentFileID = 15,

    /// <summary>The first available CNID for user's files and folders.</summary>
    kHFSFirstUserCatalogNodeID = 16
}
