namespace HfsReader;

public enum HFSKnownCatalogNodeID : uint
{
    // 0 Unknown (Reserved)
    Reserved = 0,

    // 1 kHFSRootParentID Parent identifier of the root directory (folder)
    kHFSRootParentID = 1,

    // 2 kHFSRootFolderID Directory identifier of the root directory (folder)
    kHFSRootFolderID = 2,

    //  3 kHFSExtentsFileID The extents (overflow) file
    kHFSExtentsFileID = 3,

    // 4 kHFSCatalogFileID The catalog file
    kHFSCatalogFileID = 4,

    // 5 kHFSBadBlockFileID The bad allocation block file
    kHFSBadBlockFileID = 5,

    // 6 kHFSAllocationFileID The allocation file (HFS+)
    kHFSAllocationFileID = 6,

    // 7 kHFSStartupFileID The startup file (HFS+)
    kHFSStartupFileID = 7,

    // 8 kHFSAttributesFileID The attributes file (HFS+)
    kHFSAttributesFileID = 8,

    // 14 kHFSRepairCatalogFileID Used temporarily by fsck_hfs when rebuilding the catalog file.
    kHFSRepairCatalogFileID = 14,

    // 15 kHFSBogusExtentFileID The bogus extent file Used temporarily during exchange files operations.
    kHFSBogusExtentFileID = 15,

    // 16 kHFSFirstUserCatalogNodeID The first available CNID for user’s files and folders
    kHFSFirstUserCatalogNodeID = 16
}
