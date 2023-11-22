#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using VL.Core;

namespace VL.Serialization.Raw
{
    static class BlitableUtils
    {
        public static bool IsBlitable<T>(this IHasMemory<T>? value)
        {
            return !RuntimeHelpers.IsReferenceOrContainsReferences<T>(); ;
        }
        public static bool IsBlitable<T>(this T? value)
        {
            return !RuntimeHelpers.IsReferenceOrContainsReferences<T>(); ;
        }

        public static bool IsBlitable(this Type? type)
        {

            MethodInfo? MI = typeof(RuntimeHelpers).GetMethod("IsReferenceOrContainsReferences");
            if (MI != null && type != null)
            {
                MI = MI.MakeGenericMethod(new[] { type });
                var isRef = MI.Invoke(null, new object[] { });

                if (isRef != null && !(bool)isRef)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
