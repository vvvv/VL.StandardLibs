using Stride.Core.IO;
using static Stride.Core.Storage.BundleOdbBackend;

namespace VL.Stride.Utils;

internal static class BundleUtils
{
    public static void LoadBundle(this DatabaseFileProvider databaseFileProvider, string bundleFile)
    {
        var bundleName = Path.GetFileNameWithoutExtension(bundleFile);
        var mountPoint = $"/{bundleName}";
        var bundlesPath = Path.GetDirectoryName(bundleFile);
        if (!VirtualFileSystem.DirectoryExists(mountPoint)) // or just use RemountFileSystem?
        {
            VirtualFileSystem.MountFileSystem(mountPoint, bundlesPath);
        }

        BundleResolveDelegate resolver = async name =>
        {
            if (name == bundleName)
            {
                return $"{mountPoint}/{Path.GetFileName(bundleFile)}";
            }
            return null;
        };

        var objDb = databaseFileProvider.ObjectDatabase;
        var bundleBackend = objDb.BundleBackend;
        bundleBackend.BundleResolve += resolver;

        try
        {
            var bundleLoadTask = bundleBackend.LoadBundle(bundleName, objDb.ContentIndexMap);
            bundleLoadTask.Wait();
        }
        finally
        {
            bundleBackend.BundleResolve -= resolver;
        }
    }
}
