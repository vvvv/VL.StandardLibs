using Stride.Core.IO;
using VL.Stride.Utils;

namespace VL.Stride
{
    public sealed class BundleLoader
    {
        private readonly DatabaseFileProvider databaseFileProvider;
        private readonly HashSet<string> loadedBundles = new(StringComparer.OrdinalIgnoreCase);

        public BundleLoader(DatabaseFileProvider databaseFileProvider)
        {
            this.databaseFileProvider = databaseFileProvider;
        }

        public void LoadBundle(string bundleFile)
        {
            lock (loadedBundles)
            {
                if (loadedBundles.Add(bundleFile))
                    databaseFileProvider.LoadBundle(bundleFile);
            }
        }
    }
}
