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

        public static bool HasMonadicValueEditor(this Type type)
        {
            return type.IsMonadicType() || type.IsOptional() || type.IsNullable();
        }

        public static IMonadicValueEditor<TValue, TMonad>? GetMonadicEditor<TValue, TMonad>()
        {
            if (typeof(TMonad).IsAssignableTo(typeof(IMonadicValue)))
                return Activator.CreateInstance(typeof(MonadicValueEditor<,>).MakeGenericType(typeof(TValue), typeof(TMonad))) as IMonadicValueEditor<TValue, TMonad>;

            if (typeof(TMonad).IsOptional())
                return Activator.CreateInstance(typeof(OptionalEditor<TValue>)) as IMonadicValueEditor<TValue, TMonad>;

            if (typeof(TMonad).IsNullable())
                return Activator.CreateInstance(typeof(NullableEditor<>).MakeGenericType(typeof(TValue))) as IMonadicValueEditor<TValue, TMonad>;

            return null;
        }

        public static TMonad Create<TMonad, TValue>(NodeContext nodeContext) 
            where TMonad : IMonadicValue<TValue>
        {
            return (TMonad)TMonad.Create(nodeContext);
        }

        private sealed class OptionalEditor<T> : IMonadicValueEditor<T, Optional<T>>
        {
            public Optional<T> Create(T value) => value;

            public T? GetValue(Optional<T> optional) => optional.Value;

            public bool HasValue([NotNullWhen(true)] Optional<T> optional) => optional.HasValue;

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
            public TMonad Create(TValue value)
            {
                var monad = MonadicUtils.Create<TMonad, TValue>(NodeContext.CurrentRoot);
                monad.Value = value;
                return monad;
            }

            public TValue? GetValue(TMonad monad) => monad.Value;
            public bool HasValue([NotNullWhen(true)] TMonad? monad) => monad is not null && monad.HasValue;
            public TMonad SetValue(TMonad monad, TValue? value)
            {
                monad.Value = value;
                return monad;
            }

            public bool DefaultIsNullOrNoValue => TMonad.DefaultIsNullOrNoValue;
        }
    }
}
