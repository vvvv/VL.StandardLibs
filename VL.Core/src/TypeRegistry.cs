using System;
using System.Collections.Immutable;

namespace VL.Core
{
    // TODO: If the need arises this should probably be kept per entry point / scope
    // Currently we assume that there're no double type mappings and no double adaptive implementations
    public abstract class TypeRegistry
    {
        public static TypeRegistry Default => ServiceRegistry.Global.GetService<TypeRegistry>();

        // Called by serializers to map types
        public abstract ImmutableArray<IVLTypeInfo> RegisteredTypes { get; }

        public abstract IVLTypeInfo GetTypeInfo(Type type);

        public abstract object CreateInstance(Type type, NodeContext nodeContext = default);

        public abstract object GetDefaultValue(Type type);

        public abstract Type GetTypeByName(string typeName);
    }
}
