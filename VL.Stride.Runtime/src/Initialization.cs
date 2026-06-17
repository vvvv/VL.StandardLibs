using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Shaders.Compiler;
using System.Reflection;
using System.Runtime.CompilerServices;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Lib.Basics.Resources;
using VL.Stride.Engine;
using VL.Stride.Graphics;
using VL.Stride.Rendering;
using VL.Stride.Rendering.Compositing;
using VL.Stride.Rendering.Lights;
using VL.Stride.Rendering.Materials;
using VL.Stride.Utils;
using static Stride.Core.Storage.BundleOdbBackend;
using ServiceRegistry = global::Stride.Core.ServiceRegistry;

[assembly: AssemblyInitializer(typeof(VL.Stride.Core.Initialization))]

namespace VL.Stride.Core
{
    public static class RenderDocConnector
    {
        public static RenderDocManager RenderDocManager;
        // The static ctor runs before the module initializer

        [ModuleInitializer] //needs to be called before any Skia code is called by the vvvv UI
        public static void Initialize()
        {
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--renderdoc"))
            {
                var renderDocManager = new RenderDocManager();

                // Only true if RenderDoc is installed
                if (!renderDocManager.IsInitialized)
                    return;

                // When calling this method the capture no longer works, didn't see anything in the RenderDoc UI (progress bar flashes, but no captured frame)
                //renderDocManager.Initialize();
                RenderDocManager = renderDocManager;
            }
        }
    }

    public sealed class Initialization : AssemblyInitializer<Initialization>
    {
        public static string GetPathToAssetDatabase()
        {
            // Check if we have write access to the base directory of the app
            // If yes, use it as it supports the portable case
            // If no, setup a directory in the user profile
            var appBasePath = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            if (FileSystemUtils.HasWriteAccess(appBasePath))
                return Path.Combine(appBasePath, "data");

            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (localAppDataPath is null)
                return null;

            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly is null)
                return null;

            var appName = entryAssembly.GetName().Name;
            if (appName == "vvvv" || appName == "vvvvc")
#pragma warning disable CS0436 // Type conflicts with imported type
                return Path.Combine(localAppDataPath, appName, ThisAssembly.NuGetPackageVersion, "data");
#pragma warning restore CS0436 // Type conflicts with imported type

            // User app
            var appVersion = entryAssembly.GetName().Version?.ToString();
            if (appName != null && appVersion != null)
                return Path.Combine(localAppDataPath, appName, appVersion, "data");
            if (appName != null)
                return Path.Combine(localAppDataPath, appName, "data");

            return null;
        }

        public Initialization()
        {
            var dataDir = GetPathToAssetDatabase();
            if (dataDir != null)
                ((FileSystemProvider)VirtualFileSystem.ApplicationData).ChangeBasePath(dataDir);
        }

        private void AppHost_PluginLoaded(object sender, PluginLoadedEventArgs args)
        {
            var fileProvider = GetDatabaseFileProvider();
            LoadBundle(fileProvider, args.PluginInfo);
        }

        private static void LoadBundle(DatabaseFileProvider databaseFileProvider, PluginInfo plugin)
        {
            var mountPoint = $"/{plugin.Name}";
            var bundlesPath = Path.Combine(plugin.Path, "data", "db", "bundles");
            var defaultBundlePath = Path.Combine(bundlesPath, "default.bundle");
            if (!File.Exists(defaultBundlePath))
                return;

            if (!VirtualFileSystem.DirectoryExists(mountPoint)) // or just use RemountFileSystem?
            {
                VirtualFileSystem.MountFileSystem(mountPoint, bundlesPath);
            }

            BundleResolveDelegate resolver = async bundleName =>
            {
                if (bundleName == plugin.Name)
                {
                    return $"/{plugin.Name}/default.bundle";
                }
                return null;
            };

            var objDb = databaseFileProvider.ObjectDatabase;
            var bundleBackend = objDb.BundleBackend;
            bundleBackend.BundleResolve += resolver;

            try
            {
                var bundleLoadTask = bundleBackend.LoadBundle(plugin.Name, objDb.ContentIndexMap);
                bundleLoadTask.Wait();
            }
            finally
            {
                bundleBackend.BundleResolve -= resolver;
            }
        }

        public override void Configure(AppHost appHost)
        {
            appHost.PluginLoaded += AppHost_PluginLoaded;

            var services = appHost.Services;

            services.RegisterService(GetBundleLoader());

            // Graphics device
            services.RegisterProvider(game => ResourceProvider.Return(game.GraphicsDevice));

            // Graphics context
            services.RegisterProvider(game => ResourceProvider.Return(game.GraphicsContext));

            // Input manager
            services.RegisterProvider(game => ResourceProvider.Return(game.Input));

            RegisterNodeFactories(appHost);
        }

        void RegisterNodeFactories(AppHost appHost)
        {
            // Use our own static node factory cache to manage the lifetime of our factories. The cache provided by VL itself is only per compilation.
            // The node factory cache will invalidate itself in case a factory or one of its nodes invalidates.
            // Not doing so can cause the hotswap to exchange nodes thereby causing weired crashes when for example
            // one of those nodes being re-created is the graphics compositor.

            RegisterStaticNodeFactory(appHost, "VL.Stride.Graphics.Nodes", nodeFactory =>
            {
                return GraphicsNodes.GetNodeDescriptions(nodeFactory);
            });

            RegisterStaticNodeFactory(appHost, "VL.Stride.Rendering.Nodes", nodeFactory =>
            {
                return MaterialNodes.GetNodeDescriptions(nodeFactory)
                    .Concat(LightNodes.GetNodeDescriptions(nodeFactory))
                    .Concat(CompositingNodes.GetNodeDescriptions(nodeFactory))
                    .Concat(RenderingNodes.GetNodeDescriptions(nodeFactory));
            });

            RegisterStaticNodeFactory(appHost, "VL.Stride.Engine.Nodes", nodeFactory =>
            {
                return EngineNodes.GetNodeDescriptions(nodeFactory)
                    .Concat(PhysicsNodes.GetNodeDescriptions(nodeFactory))
                    .Concat(VRNodes.GetNodeDescriptions(nodeFactory))
                    ;
            });

            RegisterStaticNodeFactory(appHost, "VL.Stride.Rendering.EffectShaderNodes", init: EffectShaderNodes.Init);
        }

        void RegisterStaticNodeFactory(AppHost appHost, string name, Func<IVLNodeDescriptionFactory, IEnumerable<IVLNodeDescription>> init)
        {
            RegisterStaticNodeFactory(appHost, name, (_, nodeFactory) => new(nodes: init(nodeFactory)));
        }

        public static void RegisterStaticNodeFactory(AppHost appHost, string name, Func<ServiceRegistry, IVLNodeDescriptionFactory, NodeBuilding.FactoryImpl> init)
        {
            lock (serviceCache)
            {
                // Only needed when running in editor! At runtime we could use the game services directly. This would simplify plugin loading.
                var strideServices = GetGlobalStrideServices();
                appHost.NodeFactoryRegistry.RegisterNodeFactory(appHost.NodeFactoryCache.GetOrAdd(name, f => init(strideServices, f)));
            }
        }

        [Obsolete]
        public static void RegisterStaticNodeFactory(IVLFactory factory, string name, Func<ServiceRegistry, IVLNodeDescriptionFactory, NodeBuilding.FactoryImpl> init)
        {
            RegisterStaticNodeFactory(factory.AppHost, name, init);
        }

        public static ServiceRegistry GetGlobalStrideServices()
        {
            lock (serviceCache)
            {
                return serviceCache.GetValue(AppHost.Global, CreateStrideServices);
            }
        }

        public static DatabaseFileProvider GetDatabaseFileProvider()
        {
            // We keep one for the entire process
            var appHost = AppHost.Global;
            return appHost.Services.GetOrAddService(_ =>
            {
                // Create and mount database file system
                var objDatabase = new ObjectDatabase(VirtualFileSystem.ApplicationDatabasePath, VirtualFileSystem.ApplicationDatabaseIndexName, VirtualFileSystem.LocalDatabasePath, 
                    loadDefaultBundle: appHost.IsExported);

                // Only set a mount path if not mounted already
                var mountPath = VirtualFileSystem.ResolveProviderUnsafe("/asset", true).Provider == null ? "/asset" : null;
                var fileProvider = new DatabaseFileProvider(objDatabase, mountPath);

                if (!appHost.IsExported)
                {
                    // Load default bundle from our package
                    var assemblyDir = Path.GetDirectoryName(typeof(Initialization).Assembly.Location);
                    var defaultBundlePath = Path.Combine(assemblyDir, "data", "db", "bundles", "default.bundle");
                    if (File.Exists(defaultBundlePath))
                        fileProvider.LoadBundle(defaultBundlePath);
                    else
                        appHost.DefaultLogger.LogWarning($"Could not find default bundle at {defaultBundlePath}");
                }

                return fileProvider;
            });
        }

        public static BundleLoader GetBundleLoader()
        {
            return AppHost.Global.Services.GetOrAddService(services =>
            {
                var fileProvider = GetDatabaseFileProvider();
                var bundleLoader = new BundleLoader(fileProvider);
                return bundleLoader;
            });
        }

        static readonly ConditionalWeakTable<AppHost, ServiceRegistry> serviceCache = new();

        // Taken from Stride/SkyboxGeneratorContext
        static ServiceRegistry CreateStrideServices(AppHost appHost)
        {
            var services = new ServiceRegistry();

            var fileProviderService = new DatabaseFileProviderService(GetDatabaseFileProvider());
            services.AddService<IDatabaseFileProviderService>(fileProviderService);

            var content = new ContentManager(services);
            services.AddService<IContentManager>(content);
            services.AddService(content);

            var graphicsDevice = GraphicsDevice.New().DisposeBy(appHost);
            var graphicsDeviceService = new GraphicsDeviceServiceLocal(services, graphicsDevice);
            services.AddService<IGraphicsDeviceService>(graphicsDeviceService);

            var graphicsContext = new GraphicsContext(graphicsDevice);
            services.AddService(graphicsContext);

            var effectSystem = new EffectSystem(services).DisposeBy(appHost);
            effectSystem.InstallEffectCompilerWithCustomPaths();

            services.AddService(effectSystem);
            effectSystem.Initialize();
            ((IContentable)effectSystem).LoadContent();
            ((EffectCompilerCache)effectSystem.Compiler).CompileEffectAsynchronously = false;

            return services;
        }
    }
}