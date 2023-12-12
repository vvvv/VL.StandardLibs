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
    [Monadic(typeof(OptionalMonadicFactory<>))]
    public readonly struct Optional<T> : IEquatable<Optional<T>>, IComparable<Optional<T>>, IOptional
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

        public override string ToString()
        {
            if (HasValue)
                return $"{Value}";

            return "No value";
        }

        public static implicit operator Optional<T>(T value) => new Optional<T>(value);

        public static bool operator ==(Optional<T> left, Optional<T> right) => left.Equals(right);

        public static bool operator !=(Optional<T> left, Optional<T> right) => !left.Equals(right);

        public static bool operator <(Optional<T> left, Optional<T> right) => left.CompareTo(right) < 0;

        public static bool operator <=(Optional<T> left, Optional<T> right) => left.CompareTo(right) <= 0;

        public static bool operator >(Optional<T> left, Optional<T> right) => left.CompareTo(right) > 0;

        public static bool operator >=(Optional<T> left, Optional<T> right) => left.CompareTo(right) >= 0;
        
        object IOptional.Object => Value;
    }


    public sealed class OptionalMonadicFactory<T> : IMonadicFactory<T, Optional<T>>
    {
        public static readonly OptionalMonadicFactory<T> Default = new OptionalMonadicFactory<T>();

        public IMonadBuilder<T, Optional<T>> GetMonadBuilder(bool isConstant)
        {
            return Builder.Instance;
        }

        sealed class Builder : IMonadBuilder<T, Optional<T>>
        {
            public static readonly Builder Instance = new Builder();
            public Optional<T> Return(T value) => new Optional<T>(value);
            public Optional<T> Default() => default;
        }
    }

#nullable enable
    public static class OptionalExtensions
    {
        public static TValue? ToNullable<TValue>(this Optional<TValue> value)
            where TValue : struct
            => value.HasValue ? value.Value : null;
        public static TValue? ToNullable_ForReferenceType<TValue>(this Optional<TValue> value)
            where TValue : class
            => value.HasValue ? value.Value : null;
    }
}
