#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace VL.Core
{
    public static class MonadicUtils
    {
        public static bool IsMonadicType(this Type type)
        {
            return type.GetCustomAttributeSafe<MonadicAttribute>() != null || type.IsOptional() || type.IsNullable();
        }

        public static bool IsOptional(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Optional<>);
        }

        public static bool IsNullable(this Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
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

        public static IMonadicValueEditor<TValue, TMonad>? GetMonadicEditor<TValue, TMonad>(this Type monadicType)
        {
            if (monadicType.IsOptional())
                return Activator.CreateInstance(typeof(OptionalEditor<TValue>)) as IMonadicValueEditor<TValue, TMonad>;

            if (monadicType.IsNullable())
                return Activator.CreateInstance(typeof(NullableEditor<>).MakeGenericType(typeof(TValue))) as IMonadicValueEditor<TValue, TMonad>;

            var factory = GetMonadicFactory<TValue, TMonad>(monadicType);
            return factory?.GetEditor();
        }

        private sealed class OptionalEditor<T> : IMonadicValueEditor<T, Optional<T>>
        {
            public Optional<T> Create(T value) => value;

            public T GetValue(Optional<T> optional) => optional.Value;

            public bool HasValue([NotNullWhen(true)] Optional<T> optional) => optional.HasValue;

            public Optional<T> SetValue(Optional<T> _, T value) => value;
        }

        private sealed class NullableEditor<T> : IMonadicValueEditor<T, T?>
            where T : struct
        {
            public T? Create(T value) => value;

            public T GetValue(T? nullable) => nullable!.Value;

            public bool HasValue([NotNullWhen(true)] T? nullable) => nullable.HasValue;

            public T? SetValue(T? _, T value) => new T?(value);
        }
    }
}
