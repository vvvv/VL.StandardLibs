#nullable enable
using System;
using System.Collections.Immutable;

namespace VL.Core
{
    public abstract class TypeRegistry
    {
        // Called by serializers to map types
        public abstract ImmutableArray<IVLTypeInfo> RegisteredTypes { get; }

        public abstract IVLTypeInfo GetTypeInfo(Type type);

        public abstract IVLTypeInfo? GetTypeByName(string typeName);

        public abstract IVLTypeInfo? GetTypeById(UniqueId id);
    }
}
