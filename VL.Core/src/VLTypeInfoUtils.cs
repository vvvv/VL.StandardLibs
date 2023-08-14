using System;
using System.Collections.Generic;
using System.Linq;

namespace VL.Core
{
    public static class VLTypeInfoUtils
    {
        private static TypeRegistry GetTypeRegistry() => AppHost.CurrentOrGlobal.TypeRegistry;

        public static IVLTypeInfo Default => GetTypeRegistry().GetTypeInfo(typeof(object));

        public static IVLTypeInfo GetVLTypeInfo(this object obj)
        {
            if (obj is null)
                return null;

            return GetTypeRegistry().GetTypeInfo(obj.GetType());
        }

        [Obsolete("Simply use IVLTypeInfo.MakeGenericType")]
        public static IVLTypeInfo MakeGenericVLTypeInfo(this IVLTypeInfo type, IReadOnlyList<IVLTypeInfo> arguments)
        {
            return type.MakeGenericType(arguments);
        }
    }
}
