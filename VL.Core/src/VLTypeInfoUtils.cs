using System;
using System.Collections.Generic;
using System.Linq;

namespace VL.Core
{
    public static class VLTypeInfoUtils
    {
        public static IVLTypeInfo Default => IAppHost.Current.Services.GetService<IVLFactory>().GetTypeInfo(typeof(object));

        public static IVLTypeInfo GetVLTypeInfo(this object obj)
        {
            if (obj is null)
                return null;

            return TypeRegistry.Default.GetTypeInfo(obj.GetType());
        }

        public static IVLTypeInfo MakeGenericVLTypeInfo(this IVLTypeInfo type, IReadOnlyList<IVLTypeInfo> arguments)
        {
            var clrType = GetGenericTypeDefinition(type.ClrType);
            if (clrType is null)
                throw new ArgumentException($"{type} is not a generic type.");

            return TypeRegistry.Default.GetTypeInfo(clrType.MakeGenericType(arguments.Select(a => a.ClrType).ToArray()));

            static Type GetGenericTypeDefinition(Type type)
            {
                if (type.IsGenericTypeDefinition)
                    return type;
                if (type.IsGenericType)
                    return type.GetGenericTypeDefinition();
                return null;
            }
        }
    }
}
