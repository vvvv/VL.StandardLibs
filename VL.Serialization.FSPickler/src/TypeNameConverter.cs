using MBrace.FsPickler;
using VL.Core;

namespace VL.Serialization.FSPickler
{

    sealed class TypeNameConverter : ITypeNameConverter
    {
        private readonly TypeRegistry typeRegistry;

        public TypeNameConverter(TypeRegistry typeRegistry)
        {
            this.typeRegistry = typeRegistry;
        }

        public TypeInfo OfSerializedType(TypeInfo value)
        {
            var assembly = System.Reflection.Assembly.Load(value.AssemblyInfo.ToAssemblyName());
            if (assembly is null)
                return value;

            var type = assembly.GetType(value.Name);
            if (type is null)
                return value;

            var typeInfo = typeRegistry.GetTypeInfo(type);
            if (typeInfo is null)
                return value;

            return new TypeInfo(ToFullName(typeInfo), assemblyInfo: value.AssemblyInfo);
        }

        public TypeInfo ToDeserializedType(TypeInfo value)
        {
            foreach (var typeInfo in typeRegistry.RegisteredTypes)
            {
                if (ToFullName(typeInfo) == value.Name)
                    return new TypeInfo(typeInfo.ClrType.FullName, AssemblyInfo.OfAssembly(typeInfo.ClrType.Assembly));
            }
            return value;
        }

        static string ToFullName(IVLTypeInfo typeInfo)
        {
            if (string.IsNullOrWhiteSpace(typeInfo.Category))
                return typeInfo.Name;
            return $"{typeInfo.Category}.{typeInfo.Name}";
        }
    }
}
