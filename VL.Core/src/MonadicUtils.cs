#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace VL.Core
{
    public static class MonadicUtils
    {
        public static bool ImplementsMonadicValue(this Type type)
        {
            return type.IsGenericType && type.IsAssignableTo(typeof(IMonadicValue));
        }

        public static bool IsOptional(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Optional<>);
        }

        public static bool IsNullable(this Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        internal static bool HasMonadicValueEditor(this Type type)
        {
            return type.ImplementsMonadicValue() || type.IsOptional() || type.IsNullable();
        }

        internal static IMonadicValueEditor<TValue, TMonad>? GetMonadicEditor<TValue, TMonad>()
        {
            if (typeof(TMonad).IsAssignableTo(typeof(IMonadicValue)))
                return Activator.CreateInstance(typeof(MonadicValueEditor<,>).MakeGenericType(typeof(TValue), typeof(TMonad))) as IMonadicValueEditor<TValue, TMonad>;

            if (typeof(TMonad).IsOptional())
                return Activator.CreateInstance(typeof(OptionalEditor<TValue>)) as IMonadicValueEditor<TValue, TMonad>;

            if (typeof(TMonad).IsNullable())
                return Activator.CreateInstance(typeof(NullableEditor<>).MakeGenericType(typeof(TValue))) as IMonadicValueEditor<TValue, TMonad>;

            return null;
        }

        public static TMonad Create<TMonad, TValue>(NodeContext nodeContext, TValue? value) 
            where TMonad : IMonadicValue<TValue>
        {
            return (TMonad)TMonad.Create(nodeContext, value);
        }

        private sealed class OptionalEditor<T> : IMonadicValueEditor<T, Optional<T>>
        {
            public Optional<T> Create(T? value) => new Optional<T>(value!);

            public T? GetValue(Optional<T> optional) => optional.Value;

            public bool HasValue(Optional<T> optional) => optional.HasValue;

            public Optional<T> SetValue(Optional<T> _, T? value) => new Optional<T>(value!);
        }

        private sealed class NullableEditor<T> : IMonadicValueEditor<T, T?>
            where T : struct
        {
            public T? Create(T value) => value;
            public T GetValue(T? nullable) => nullable!.Value;
            public bool HasValue([NotNullWhen(true)] T? nullable) => nullable.HasValue;
            public T? SetValue(T? _, T value) => new T?(value);
        }

        private sealed class MonadicValueEditor<TValue, TMonad> : IMonadicValueEditor<TValue, TMonad>
            where TMonad : IMonadicValue<TValue>
        {
            public TMonad Create(TValue? value)
            {
                return MonadicUtils.Create<TMonad, TValue>(NodeContext.CurrentRoot, value);
            }

            public TValue? GetValue(TMonad monad) => monad.Value;
            public bool HasValue(TMonad? monad) => monad is not null && monad.HasValue;
            public TMonad SetValue(TMonad monad, TValue? value)
            {
                if (monad.AcceptsValue)
                {
                    monad.Value = value;
                    return monad;
                }
                else
                {
                    return Create(value);
                }
            }

            public bool HasCustomDefault => TMonad.HasCustomDefault;
        }
    }
}
