using Stride.Core;
using Stride.Core.IO;
using Stride.Engine;
using static Stride.Core.Storage.BundleOdbBackend;

namespace VL.Stride
{
    public sealed class BundleLoader
    {
        private readonly List<string> bundles = new();

        public void AddBundle(string bundleFile)
        {
            bundles.Add(bundleFile);

            var servicesUsedByNodeFactories = VL.Stride.Core.Initialization.GetGlobalStrideServices();
            LoadBundle(servicesUsedByNodeFactories, bundleFile);
        }

        internal void LoadBundles(Game game)
        {
            foreach (var bundle in bundles) 
                LoadBundle(game, bundle);
        }

        private void LoadBundle(Game game, string bundleFile)
        {
            var services = game.Services;
            LoadBundle(services, bundleFile);
        }

        private void LoadBundle(ServiceRegistry services, string bundleFile)
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

            var objDb = services.GetService<IDatabaseFileProviderService>().FileProvider.ObjectDatabase;
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
}
