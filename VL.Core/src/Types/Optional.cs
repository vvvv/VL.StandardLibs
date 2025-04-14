using System;
using System.Collections.Generic;

namespace VL.Core
{
    /// <summary>
    /// a non-generic View onto Optional
    /// </summary>
    public interface IOptional
    {
        /// <summary>
        /// The Value as an object
        /// </summary>
        public object Object { get; }

        /// <summary>
        /// Whether or not a value is present.
        /// </summary>
        public bool HasValue { get; }
    }

    /// <summary>
    /// Represents an optional value. Use HasValue to test whether a value is present.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    [Serializable]
    public readonly struct Optional<T> : IEquatable<Optional<T>>, IComparable<Optional<T>>, IOptional, IEquatable<IOptional>
    {
        public Optional(T value)
        {
            Value = value;
            HasValue = true;
        }

        /// <summary>
        /// The actual value.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Whether or not a value is present.
        /// </summary>
        public bool HasValue { get; }

        /// <summary>
        /// Whether or not the value is missing.
        /// </summary>
        public bool HasNoValue => !HasValue;

        public bool Equals(Optional<T> other)
        {
            if (!HasValue && !other.HasValue)
                return true;

            if (HasValue && other.HasValue)
                return EqualityComparer<T>.Default.Equals(Value, other.Value);

            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is Optional<T> optional)
                return Equals(optional);

            return false;
        }

        public override int GetHashCode()
        {
            if (HasValue)
                return Value is null ? 1 : Value.GetHashCode();

            return 0;
        }

        public int CompareTo(Optional<T> other)
        {
            if (HasValue && !other.HasValue) 
                return 1;

            if (!HasValue && other.HasValue) 
                return -1;

            return Comparer<T>.Default.Compare(Value, other.Value);
        }

        public bool Equals(IOptional other)
        {
            if (other is Optional<T>)
            {
                var otherValue = (Optional<T>)other;
                return Equals(otherValue);
            }
            return false;
        }

        public override string ToString()
        {
            if (HasValue)
                return $"{Value}";

            return "No value";
        }

        public T TryGetValue (T fallbackValue)
        {
            if (HasValue)
            {
                return Value;
            }

            return fallbackValue;
        }

        public static implicit operator Optional<T>(T value) => new Optional<T>(value);

        public static bool operator ==(Optional<T> left, Optional<T> right) => left.Equals(right);

        public static bool operator !=(Optional<T> left, Optional<T> right) => !left.Equals(right);

        public static bool operator <(Optional<T> left, Optional<T> right) => left.CompareTo(right) < 0;

        public static bool operator <=(Optional<T> left, Optional<T> right) => left.CompareTo(right) <= 0;

        public static bool operator >(Optional<T> left, Optional<T> right) => left.CompareTo(right) > 0;

        public static bool operator >=(Optional<T> left, Optional<T> right) => left.CompareTo(right) >= 0;
        
        object IOptional.Object => Value;

        public static Optional<T> Select(object value, Func<object, T> selector)
        {
            if (value != null)
                return new Optional<T>(selector(value));
            else
                return new Optional<T>();
        }
    }

#nullable enable
    public static class OptionalExtensions
    {
        public static Optional<TValue> ToOptional<TValue>(this TValue? value)
            where TValue : struct
            => value.HasValue ? value.Value : new Optional<TValue>();

        public static TValue? ToNullable<TValue>(this Optional<TValue> value)
            where TValue : struct
            => value.HasValue ? value.Value : null;
        
        public static TValue? ToNullable_ForReferenceType<TValue>(this Optional<TValue> value)
            where TValue : class
            => value.HasValue ? value.Value : null;

        public static TValue ValueOrDefault<TValue>(this Optional<TValue> value, TValue @default = default)
            where TValue : struct
            => value.HasValue ? value.Value : @default; 
        
        public static TValue? ValueOrDefault_ForReferenceType<TValue>(this Optional<TValue> value, TValue? @default = null)
            where TValue : class
            => value.HasValue ? value.Value : @default;

        public static Optional<B> Project<A, B>(this Optional<A> value, Func<A, B> projection)
            => value.HasValue ? new Optional<B>(projection(value.Value)) : new Optional<B>();

        public static IOptional CreateOptional(Type t)
        {
            var optionalType = typeof(Optional<>).MakeGenericType(t);
            var optional = Activator.CreateInstance(optionalType);
            return (IOptional)optional;
        }

        public static IOptional CreateOptional(object value, Type t)
        {
            var optionalType = typeof(Optional<>).MakeGenericType(t);
            var optional = Activator.CreateInstance(optionalType, value);
            return (IOptional)optional;
        }

        public static IOptional Select(object value, Func<object, object> selector, Type type)
        {
            if (value != null)
                return OptionalExtensions.CreateOptional(selector(value), type);
            else
                return OptionalExtensions.CreateOptional(type);
        }
    }
}
