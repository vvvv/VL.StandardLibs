#nullable enable
using System.Collections.Immutable;

namespace VL.Stride.Utils;

/// <summary>
/// The shader database stores the absolute path each shader had when its bundle was built (in <c>shaders/Name.sdsl/path</c>).
/// Stride loads a shader from that path instead of the database whenever the file exists, so the shaders of a package built
/// locally can be edited live. For a package built elsewhere the path belongs to the build machine and a file found there
/// is unrelated (an old checkout for example), so a recorded path is only used if it lies inside the package of a loaded bundle.
/// </summary>
internal static class BundleSourcePaths
{
    /// <summary>
    /// Appended by Stride to the URL of a shader to store its source path (see ImportStreamCommand in Stride).
    /// </summary>
    public const string UrlSuffix = "/path";

    private static ImmutableHashSet<string> packageDirectories = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Allows the recorded source paths that lie inside the package of the given bundle file.
    /// </summary>
    public static void AddBundle(string bundleFile)
    {
        var packageDirectory = GetPackageDirectory(bundleFile);
        if (packageDirectory != null)
            ImmutableInterlocked.Update(ref packageDirectories, d => d.Add(packageDirectory));
    }

    /// <summary>
    /// Whether a source path recorded in the database lies inside the package of a loaded bundle.
    /// </summary>
    public static bool IsInsidePackageOfLoadedBundle(string recordedPath)
    {
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(recordedPath);
        }
        catch (Exception)
        {
            return false;
        }

        return packageDirectories.Any(d => fullPath.StartsWith(Path.TrimEndingDirectorySeparator(d) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The package directory of a bundle file in <c>package/lib/framework/data/db/bundles</c>, or null if the bundle is not in a package.
    /// </summary>
    private static string? GetPackageDirectory(string bundleFile)
    {
        var libDirectory = Directory.GetParent(Path.GetFullPath(bundleFile))?.Parent?.Parent?.Parent?.Parent;
        if (libDirectory is null || !string.Equals(libDirectory.Name, "lib", StringComparison.OrdinalIgnoreCase))
            return null;
        return libDirectory.Parent?.FullName;
    }
}
