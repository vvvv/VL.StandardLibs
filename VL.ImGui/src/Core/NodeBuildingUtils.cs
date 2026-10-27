using System.Collections.Immutable;
using System.Reflection;
using VL.Core;

namespace VL.ImGui
{
    internal static class NodeBuildingUtils
    {
        public static void RegisterGeneratedNodes(AppHost appHost, string name, Func<IVLNodeDescriptionFactory, IEnumerable<IVLNodeDescription>> getNodeDescriptions)
        {
            appHost.NodeFactoryRegistry.RegisterNodeFactory(appHost.NodeFactoryCache.GetOrAdd(name, f =>
            {
                var nodes = getNodeDescriptions(f).ToImmutableArray();
                return NodeBuilding.NewFactoryImpl(nodes);
            }));
        }

        public static IVLPinDescription Input<T>(this NodeBuilding.NodeDescriptionBuildContext c, string name, T witness, object? defaultValue = null, string? summary = null, string? remarks = null)
        {
            return c.Pin(name, typeof(T), defaultValue, summary, remarks);
        }

        public static IVLPinDescription Output<T>(this NodeBuilding.NodeDescriptionBuildContext c, string name, T? witness = default, string? summary = null, string? remarks = null)
        {
            return c.Pin(name, typeof(T), summary, remarks);
        }
    }
}
