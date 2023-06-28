using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Shaders.Compiler;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Lib.Basics.Resources;
using VL.Stride.Engine;
using VL.Stride.Graphics;
using VL.Stride.Rendering;
using VL.Stride.Rendering.Compositing;
using VL.Stride.Rendering.Lights;
using VL.Stride.Rendering.Materials;

using ServiceRegistry = global::Stride.Core.ServiceRegistry;

[assembly: AssemblyInitializer(typeof(VL.Stride.Core.Initialization))]

namespace VL.Stride.Core
{
    public sealed class Initialization : AssemblyInitializer<Initialization>
    {
        public static string GetPathToAssetDatabase()
        {
            var thisDirectory = Path.GetDirectoryName(typeof(Initialization).Assembly.Location);
            var dataDir = Path.Combine(thisDirectory, "data");
            if (Directory.Exists(dataDir))
                return dataDir;
            else
            {
                // Are we running as source package?
                dataDir = Path.Combine(thisDirectory, "..", "..", "..", "VL.Stride", "lib", "net6.0-windows", "data");
                if (Directory.Exists(dataDir))
                    return dataDir;
            }
            // Let Stride figure it out
            return null;
        }

        public Initialization()
        {
            var dataDir = GetPathToAssetDatabase();
            if (dataDir != null)
                ((FileSystemProvider)VirtualFileSystem.ApplicationData).ChangeBasePath(dataDir);
        }

        protected override void RegisterServices(IVLFactory factory)
        {
            var services = VL.Core.ServiceRegistry.Current;

            // Graphics device
            services.RegisterProvider(game => ResourceProvider.Return(game.GraphicsDevice));

            // Graphics context
            services.RegisterProvider(game => ResourceProvider.Return(game.GraphicsContext));

            // Input manager
            services.RegisterProvider(game => ResourceProvider.Return(game.Input));

            RegisterNodeFactories(factory);
        }

        void RegisterNodeFactories(IVLFactory factory)
        {
            // Use our own static node factory cache to manage the lifetime of our factories. The cache provided by VL itself is only per compilation.
            // The node factory cache will invalidate itself in case a factory or one of its nodes invalidates.
            // Not doing so can cause the hotswap to exchange nodes thereby causing weired crashes when for example
            // one of those nodes being re-created is the graphics compositor.

            RegisterStaticNodeFactory(factory, "VL.Stride.Graphics.Nodes", nodeFactory =>
            {
                return GraphicsNodes.GetNodeDescriptions(nodeFactory);
            });

            RegisterStaticNodeFactory(factory, "VL.Stride.Rendering.Nodes", nodeFactory =>
            {
                return MaterialNodes.GetNodeDescriptions(nodeFactory)
                    .Concat(LightNodes.GetNodeDescriptions(nodeFactory))
                    .Concat(CompositingNodes.GetNodeDescriptions(nodeFactory))
                    .Concat(RenderingNodes.GetNodeDescriptions(nodeFactory));
            });

            RegisterStaticNodeFactory(factory, "VL.Stride.Engine.Nodes", nodeFactory =>
            {
                return EngineNodes.GetNodeDescriptions(nodeFactory)
                    .Concat(PhysicsNodes.GetNodeDescriptions(nodeFactory))
                    .Concat(VRNodes.GetNodeDescriptions(nodeFactory))
                    ;
            });

            RegisterStaticNodeFactory(factory, "VL.Stride.Rendering.EffectShaderNodes", init: EffectShaderNodes.Init);
        }

        void RegisterStaticNodeFactory(IVLFactory factory, string name, Func<IVLNodeDescriptionFactory, IEnumerable<IVLNodeDescription>> init)
        {
            RegisterStaticNodeFactory(factory, name, (_, nodeFactory) => new(nodes: init(nodeFactory)));
        }

        public static void RegisterStaticNodeFactory(IVLFactory factory, string name, Func<ServiceRegistry, IVLNodeDescriptionFactory, NodeBuilding.FactoryImpl> init)
        {
            lock (serviceCache)
            {
                var strideServices = GetGlobalStrideServices();
                factory.RegisterNodeFactory(NodeBuilding.NewNodeFactory(factory, name, f => init(strideServices, f)));
            }
        }

        public static ServiceRegistry GetGlobalStrideServices()
        {
            lock (serviceCache)
            {
                var root = VL.Core.ServiceRegistry.CurrentOrGlobal.GetService<CompositeDisposable>();
                return serviceCache.GetValue(root, CreateStrideServices);
            }
        }

        static readonly ConditionalWeakTable<CompositeDisposable, ServiceRegistry> serviceCache = new ConditionalWeakTable<CompositeDisposable, ServiceRegistry>();

        // Taken from Stride/SkyboxGeneratorContext
        static ServiceRegistry CreateStrideServices(CompositeDisposable subscriptions)
        {
            var services = new ServiceRegistry();

            var fileProvider = InitializeAssetDatabase();
            var fileProviderService = new DatabaseFileProviderService(fileProvider);
            services.AddService<IDatabaseFileProviderService>(fileProviderService);

            var content = new ContentManager(services);
            services.AddService<IContentManager>(content);
            services.AddService(content);

            var graphicsDevice = GraphicsDevice.New();
            var graphicsDeviceService = new GraphicsDeviceServiceLocal(services, graphicsDevice);
            services.AddService<IGraphicsDeviceService>(graphicsDeviceService);

            var graphicsContext = new GraphicsContext(graphicsDevice);
            services.AddService(graphicsContext);

            var effectSystem = new EffectSystem(services);
            effectSystem.InstallEffectCompilerWithCustomPaths();

            services.AddService(effectSystem);
            effectSystem.Initialize();
            ((IContentable)effectSystem).LoadContent();
            ((EffectCompilerCache)effectSystem.Compiler).CompileEffectAsynchronously = false;

            subscriptions?.Add(effectSystem);
            subscriptions?.Add(graphicsDevice);

            return services;
        }

        // Taken from Stride/Game
        static DatabaseFileProvider InitializeAssetDatabase()
        {
            // Create and mount database file system
            var objDatabase = ObjectDatabase.CreateDefaultDatabase();

            // Only set a mount path if not mounted already
            var mountPath = VirtualFileSystem.ResolveProviderUnsafe("/asset", true).Provider == null ? "/asset" : null;
            return new DatabaseFileProvider(objDatabase, mountPath);
        }
    }
}