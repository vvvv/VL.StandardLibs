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
            const string proxySuffix = "+Proxy";

            var name = value.Name;
            if (name.EndsWith(proxySuffix))
                name = name.Substring(0, name.Length - proxySuffix.Length);

            foreach (var typeInfo in typeRegistry.RegisteredTypes)
            {
                if (typeInfo.ClrType.FullName == name)
                    return new TypeInfo(ToFullName(typeInfo), assemblyInfo: value.AssemblyInfo);
            }
            return value;
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
