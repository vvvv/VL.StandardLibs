using System.Collections.Immutable;
using System.Reflection;
using VL.Core;

namespace VL.ImGui
{
    internal static class NodeBuildingUtils
    {
        public static void RegisterGeneratedNodes(IVLFactory factory, string name, Assembly assembly)
        {
            factory.RegisterNodeFactory(NodeBuilding.NewNodeFactory(factory, name, f =>
            {
                var nodes = GetNodes(f, assembly).ToImmutableArray();
                return NodeBuilding.NewFactoryImpl(nodes);
            }));
        }

        public static IEnumerable<IVLNodeDescription> GetNodes(IVLNodeDescriptionFactory factory, Assembly assembly)
        {
            var result = new List<MethodInfo>();
            foreach (var type in assembly.GetTypes())
            {
                var attr = type.GetCustomAttribute<GenerateNodeAttribute>();
                if (attr is null)
                    continue;

                if (attr.GenerateRetained)
                    AddMethod(result, type, "GetNodeDescription_RetainedMode");
                if (attr.GenerateImmediate)
                    AddMethod(result, type, "GetNodeDescription_ImmediateMode");
            }

            foreach (var m in result)
            {
                if (m.Invoke(null, new[] { factory }) is IVLNodeDescription n)
                    yield return n;
            }

            static void AddMethod(List<MethodInfo> result, Type type, string methodName)
            {
                var m = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
                if (m is null)
                    throw new Exception($"{methodName} not found");
                result.Add(m);
            }
        }

        public static IVLPinDescription Input<T>(this NodeBuilding.NodeDescriptionBuildContext c, string name, T defaultValue, string? summary = null, string? remarks = null)
        {
            return c.Pin(name, typeof(T), defaultValue, summary, remarks);
        }

        // Special overload for pins of type Optional<T> - we explicitly need to set the VL default value to null
        public static IVLPinDescription Input<T>(this NodeBuilding.NodeDescriptionBuildContext c, string name, Optional<T> defaultValue, string? summary = null, string? remarks = null)
        {
            return c.Pin(name, typeof(Optional<T>), null, summary, remarks);
        }

        public static IVLPinDescription Output<T>(this NodeBuilding.NodeDescriptionBuildContext c, string name, T? witness = default, string? summary = null, string? remarks = null)
        {
            return c.Pin(name, typeof(T), summary, remarks);
        }
    }
}
