using System.Buffers.Binary;
using System.Diagnostics;

namespace HfsReader;

public struct HFSFinderInformation
{
    public uint BootableSystemDirectoryId { get; }

    public uint ParentIdentifier { get; }

    public uint MountWindowDirectoryId { get; }

    public uint SystemMacOS89DirectoryId { get; }

    public uint Reserved { get; }

    public uint SystemMacOSXDirectoryId { get; }

    public ulong SystemVolumeIdentifier { get; }

    public HFSFinderInformation(Span<byte> data)
    {
        if (data.Length != 32)
        {
            throw new ArgumentException("Finder Information data must be exactly 32 bytes long.", nameof(data));
        }

        int offset = 0;

        // Contains the directory identifier of the directory containing the bootable system.
        // I.e. "System Folder" in Mac OS 8 or 9, or "/System/Library/CoreServices" in Mac
        //  OS X.
        // It is zero if there is no bootable system on the volume. Typically this
        // value equals the value in entry 3 or 5.
        BootableSystemDirectoryId = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Contains the parent identifier of the startup application, i.e. "Finder".
        // The value is zero if the volume is not bootable.
        ParentIdentifier = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Contains the directory identifier of a directory whose window should be
        // displayed in the Finder when the volume is mounted, or zero if no directory
        // window should be opened.
        // In classic Mac OS, this is the first in a linked list of windows to open;
        // the frOpenChain field of the directory’s Finder Info contains the next
        // directory ID in the list. The open window list is deprecated. The
        // Mac OS X Finder will open this directory’s window, but ignores the rest of
        // the open window list. The Mac OS X Finder does not modify this field.
        MountWindowDirectoryId = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Contains the directory identifier of a bootable Mac OS 8 or 9 System Folder,
        // or zero if not available.
        SystemMacOS89DirectoryId = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Unknown (Reserved)
        Reserved = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Contains the directory identifier of a bootable Mac OS X system, the
        // "/System/Library/CoreServices" directory, or zero if not available.
        SystemMacOSXDirectoryId = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));
        offset += 4;

        // Used by Mac OS X to store an unique 64-bit volume identifier.
        // This identifier is used for tracking whether a given volume’s ownership
        // (user identifier) information should be honored.
        // These elements may be zero if no such identifier has been created for
        // the volume.
        SystemVolumeIdentifier = BinaryPrimitives.ReadUInt64BigEndian(data.Slice(offset));
        offset += 8;

        Debug.Assert(offset == data.Length);
    }
}
