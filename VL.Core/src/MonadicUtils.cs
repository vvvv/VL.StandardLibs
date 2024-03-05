#nullable enable

using System;
using System.Reflection;

namespace VL.Core
{
    public static class MonadicUtils
    {
        public static bool IsMonadicType(this Type type)
        {
            return type.GetCustomAttributeSafe<MonadicAttribute>() != null || Nullable.GetUnderlyingType(type) != null;
        }

        public static Type? GetMonadicFactoryType(this Type monadicType, Type valueType)
        {
            var attribute = monadicType.GetCustomAttributeSafe<MonadicAttribute>();
            var factoryType = attribute?.Factory;

            if (factoryType is null)
            {
                if (monadicType.IsGenericType && monadicType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    factoryType = typeof(NullableMonadicFactory<>);
            }

            return factoryType?.MakeGenericType(valueType);
        }

        public static IMonadicFactory<TValue, TMonad>? GetMonadicFactory<TValue, TMonad>(this Type monadicType)
        {
            var factoryType = monadicType.GetMonadicFactoryType(typeof(TValue));
            if (factoryType is null)
                return null;

            return Activator.CreateInstance(factoryType) as IMonadicFactory<TValue, TMonad>;
        }
    }
}
