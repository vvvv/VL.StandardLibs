#nullable enable

using System;
using System.Reflection;

namespace VL.Core
{
    public static class MonadicUtils
    {
        public static bool IsMonadicType(this Type type)
        {
            return type.GetCustomAttributeSafe<MonadicAttribute>() != null;
        }

        public static Type? GetMonadicFactoryType(this Type monadicType, Type valueType)
        {
            var attribute = monadicType.GetCustomAttributeSafe<MonadicAttribute>();
            var factoryType = attribute?.Factory;
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
