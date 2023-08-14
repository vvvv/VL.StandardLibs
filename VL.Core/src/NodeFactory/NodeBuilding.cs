using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace VL.Core
{
    public static partial class NodeBuilding
    {
        [Obsolete("Use AppHost.NodeFactoryRegistry.RegisterNodeFactory")]
        public static void RegisterNodeFactory(this IVLFactory services, IVLNodeDescriptionFactory factory)
        {
            services.AppHost.NodeFactoryRegistry.RegisterNodeFactory(factory);
        }

        [Obsolete("Use the appHost overload instead")]
        public static void RegisterNodeFactory(this IVLFactory services,
            string identifier,
            Func<IVLNodeDescriptionFactory, FactoryImpl> init)
        {
            services.AppHost.RegisterNodeFactory(identifier, init);
        }

        [Obsolete("Use the appHost overload instead")]
        public static void RegisterNodeFactory(this IVLFactory services,
            string identifier,
            Func<string, IVLNodeDescriptionFactory, FactoryImpl> forPath)
        {
            services.AppHost.RegisterNodeFactory(identifier, forPath);
        }

        [Obsolete("Use the NodeFactoryCache.GetOrAdd extension method")]
        public static IVLNodeDescriptionFactory NewNodeFactory(
            IVLFactory services,
            string identifier,
            Func<IVLNodeDescriptionFactory, FactoryImpl> init)
        {
            return services.AppHost.NodeFactoryCache.GetOrAdd(identifier, init);
        }

        public static void RegisterNodeFactory(this AppHost appHost,
            string identifier,
            Func<IVLNodeDescriptionFactory, FactoryImpl> init)
        {
            var nodeFactory = appHost.NodeFactoryCache.GetOrAdd(identifier, init);
            appHost.NodeFactoryRegistry.RegisterNodeFactory(nodeFactory);
        }

        public static void RegisterNodeFactory(this AppHost appHost,
            string identifier,
            Func<string, IVLNodeDescriptionFactory, FactoryImpl> forPath)
        {
            var nodeFactory = appHost.NodeFactoryCache.GetOrAdd(identifier, _ => new FactoryImpl(forPath: p => f => forPath(p, f)));
            appHost.NodeFactoryRegistry.RegisterNodeFactory(nodeFactory);
        }

        public static IVLNodeDescriptionFactory GetOrAdd(
            this NodeFactoryCache nodeFactoryCache,
            string identifier,
            Func<IVLNodeDescriptionFactory, FactoryImpl> init)
        {
            return nodeFactoryCache.GetOrAdd(identifier, () =>
            {
                return new DelegateNodeDescriptionFactory(nodeFactoryCache, identifier, init);
            });
        }

        public static FactoryImpl NewFactoryImpl(ImmutableArray<IVLNodeDescription> nodes = default, IObservable<object> invalidated = default, Func<string, Func<IVLNodeDescriptionFactory, FactoryImpl>> forPath = default, Action<ExportContext> export = default)
        {
            return new FactoryImpl(nodes.IsDefault ? Enumerable.Empty<IVLNodeDescription>() : nodes, invalidated, forPath, export);
        }

        public static IVLNodeDescription NewNodeDescription(this IVLNodeDescriptionFactory factory,
            string name,
            string category,
            bool fragmented,
            Func<NodeDescriptionBuildContext, NodeImplementation> init)
        {
            return new NodeDescription(factory, name, category, fragmented, invalidated: default, init);
        }

        public static IVLNodeDescription NewNodeDescription(this IVLNodeDescriptionFactory factory,
            string name,
            string category,
            bool fragmented,
            IObservable<object> invalidated,
            Func<NodeDescriptionBuildContext, NodeImplementation> init)
        {
            return new NodeDescription(factory, name, category, fragmented, invalidated, init);
        }

        public static IVLNodeDescription NewNodeDescription(this IVLNodeDescriptionFactory factory,
            string name,
            string category,
            bool fragmented,
            IObservable<object> invalidated,
            Func<NodeDescriptionBuildContext, NodeImplementation> init,
            string tags)
        {
            return new NodeDescription(factory, name, category, fragmented, invalidated, init, tags);
        }

        public static IObservable<FileSystemEventArgs> WatchDir(string dir) => FileSystemUtils.WatchDir(dir);
    }
}
