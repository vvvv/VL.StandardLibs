#nullable enable

using Stride.Core;
using Stride.Core.IO;
using System.Runtime.CompilerServices;

namespace VL.Stride.Utils;

public static class UPathUtils
{
    /// <summary>
    /// Creates a new <see cref="UFile"/> from the specified string value. Takes care of network paths.
    /// </summary>
    public static UFile? CreateUFile(this string? value)
    {
        UFile? file = value;
        file?.ApplyNetworkPathFix(value!);
        return file;
    }

    /// <summary>
    /// Creates a <see cref="UDirectory"/> instance from the specified string value. Takes care of network paths.
    /// </summary>
    public static UDirectory? CreateUDirectory(this string? value)
    {
        UDirectory? directory = value;
        directory?.ApplyNetworkPathFix(value!);
        return directory;
    }

    // Workaround for network paths
    private static void ApplyNetworkPathFix(this UPath path, string value)
    {
        if (value.StartsWith(@"\\"))
        {
            ref var fullPath = ref GetFullPathBackingField(path);
            fullPath = $"{UPath.DirectorySeparatorString}{path.FullPath}";

            // Adjust spans accordingly
            ref var driveSpan = ref GetDriveSpan(path);
            driveSpan = new StringSpan(0, 2); // To make HasDrive return true (otherwise asset system complains)
            ref var directorySpan = ref GetDirectorySpan(path);
            directorySpan = new StringSpan(directorySpan.Start, directorySpan.Length + 1);
            ref var nameSpan = ref GetNameSpan(path);
            nameSpan = new StringSpan(nameSpan.Start + 1, nameSpan.Length);
            ref var extensionSpan = ref GetExtensionSpan(path);
            extensionSpan = new StringSpan(extensionSpan.Start + 1, extensionSpan.Length);
        }

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<FullPath>k__BackingField")]
        extern static ref string GetFullPathBackingField(UPath path);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "DriveSpan")]
        extern static ref StringSpan GetDriveSpan(UPath path);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "DirectorySpan")]
        extern static ref StringSpan GetDirectorySpan(UPath path);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "NameSpan")]
        extern static ref StringSpan GetNameSpan(UPath path);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "ExtensionSpan")]
        extern static ref StringSpan GetExtensionSpan(UPath path);
    }
}
