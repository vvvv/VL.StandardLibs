using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;

namespace VL.Core
{
    public static partial class NodeBuilding
    {
        public static void RegisterNodeFactory(this IVLFactory services, IVLNodeDescriptionFactory factory)
        {
            var registry = services.GetService<NodeFactoryRegistry>();
            registry.RegisterNodeFactory(factory);
        }

        public static void RegisterNodeFactory(this IVLFactory services,
            string identifier,
            Func<IVLNodeDescriptionFactory, FactoryImpl> init)
        {
            RegisterNodeFactory(services, NewNodeFactory(services, identifier, init));
        }

        public static void RegisterNodeFactory(this IVLFactory services,
            string identifier,
            Func<string, IVLNodeDescriptionFactory, FactoryImpl> forPath)
        {
            RegisterNodeFactory(services, NewNodeFactory(services, identifier, _ => new FactoryImpl(forPath: p => f => forPath(p, f))));
        }

        public static IVLNodeDescriptionFactory NewNodeFactory(
            IVLFactory services,
            string identifier,
            Func<IVLNodeDescriptionFactory, FactoryImpl> init)
        {
            var nodeFactoryCache = services.GetService<NodeFactoryCache>();
            return nodeFactoryCache.GetOrAdd(identifier, () =>
            {
                return new DelegateNodeDescriptionFactory(nodeFactoryCache, identifier, init);
            });
        }

        public static FactoryImpl NewFactoryImpl(ImmutableArray<IVLNodeDescription> nodes = default, IObservable<object> invalidated = default, Func<string, Func<IVLNodeDescriptionFactory, FactoryImpl>> forPath = default, Action<ExportContext> export = default)
        {
            return new FactoryImpl(nodes, invalidated, forPath, export);
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

        public static IVLNode CreateNode(this IVLFactory factory, NodeContext context, string name, string category)
        {
            var nodeDesc = factory.GetNodeDescription(name, category);
            if (nodeDesc is null)
                throw new ArgumentException($"Node \"{name} [{category}]\" not found.");

            return nodeDesc.CreateInstance(context);
        }

        public static IVLNodeDescription GetNodeDescription(this IVLFactory factory, string name, string category)
        {
            var registry = factory.GetService<NodeFactoryRegistry>();
            foreach (var nodeFactory in registry.Factories)
            {
                var nodeDesc = nodeFactory.NodeDescriptions.FirstOrDefault(d => d.Name == name && d.Category == category);
                if (nodeDesc != null)
                    return nodeDesc;
            }
            return null;
        }

        public static IObservable<FileSystemEventArgs> WatchDir(string dir) => FileSystemUtils.WatchDir(dir);
    }
}
