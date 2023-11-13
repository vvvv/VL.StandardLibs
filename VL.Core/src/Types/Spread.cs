using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Core.Utils;

namespace VL.Lib.Collections
{
    /// <summary>
    /// Non-generic spread interface.
    /// </summary>
    public interface ISpread : ICollection
    {
        object GetItem(int index);
        ISpread SetItem(int index, object value);
        ISpreadBuilder ToBuilder();
        Type ElementType { get; }
    }

    public class SpreadMonadFactory<T> : IMonadicFactory<T, Spread<T>>
    {
        public static readonly SpreadMonadFactory<T> Default = new SpreadMonadFactory<T>();

        public Builder GetMonadBuilder()
        {
            return new Builder();
        }

        IMonadBuilder<T, Spread<T>> IMonadicFactory<T, Spread<T>>.GetMonadBuilder(bool isConstant)
        {
            return GetMonadBuilder();
        }

        public struct Builder : IMonadBuilder<T, Spread<T>>
        {
            private Spread<T> spread;

            public Spread<T> Return(T value)
            {
                var builder = CollectionBuilders.GetBuilder(spread, 1);
                builder.Add(value);
                return spread = builder.Commit();
            }
        }
    }

    [Serializable]
    // TBD: The following line would allow connecting single values to spreads - do we want this? What would be the implications?
    //[Monadic(typeof(SpreadMonadFactory<>))]
    public sealed class Spread<T> : IReadOnlyList<T>, IHasMemory<T>, ISpread, IList<T> /* LINQ looks for IList and not IReadOnlyList */
    {
        public static readonly Spread<T> Empty = new Spread<T>(ImmutableArray<T>.Empty);

        public static Spread<T> CastUp<TDerived>(Spread<TDerived> source) where TDerived : class, T => new Spread<T>(ImmutableArray<T>.CastUp(source._array));

        // Compiler accesses this array directly due to lacking optimizations of JIT compiler:
        // Neither GetEnumerator/MoveNext/Current nor for (...; i < input.Count;) produce same good code as direct access on array does :/
        public readonly ImmutableArray<T> _array;

        internal Spread(ImmutableArray<T> array)
        {
            _array = array;
        }

        /// <summary>
        /// Creates a new read-only memory region over this spread.
        /// </summary>
        /// <returns>The read-only memory representation of this spread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<T> AsMemory() => _array.AsMemory();

        /// <summary>
        /// Creates a new read-only span region over this spread.
        /// </summary>
        /// <returns>The read-only span representation of this spread.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsSpan() => _array.AsSpan();

        /// <summary>
        /// Creates a spread builder with the same contents as this spread that can be efficiently modified across multiple operations
        /// </summary>
        /// <returns></returns>
        public SpreadBuilder<T> ToBuilder()
        {
            return new SpreadBuilder<T>(this);
        }

        /// <summary>
        /// Whether or not the spread is empty
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _array.IsEmpty; }
        }

        /// <summary>
        /// Returns the number of slices in the spread
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _array.Length; }
        }

        /// <summary>
        /// Returns the item at the specified index. Can throw ArgumentOutOfRangeException exception
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _array[index]; }
        }

        /// <summary>
        /// Searches for the specified item and returns the index of the first occurrence
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(T item)
        {
            return _array.IndexOf(item);
        }

        #region IEnumerable<T> members

        public ImmutableArray<T>.Enumerator GetEnumerator()
        {
            return _array.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_array).GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return ((IEnumerable<T>)_array).GetEnumerator();
        }

        #endregion

        #region ISpread members

        object ISpread.GetItem(int index) => this[index];

        ISpread ISpread.SetItem(int index, object value)
        {
            var builder = this.ToBuilder();
            builder[index] = (T)value;
            return builder.ToSpread();
        }

        ISpreadBuilder ISpread.ToBuilder() => ToBuilder();

        Type ISpread.ElementType => typeof(T);

        #endregion

        // State restorer casts to ICollection in order to get Count and Enumerator.
        #region ICollection members

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)_array).CopyTo(array, index);
        }

        int ICollection.Count
        {
            get { return Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return ((ICollection)_array).IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return ((ICollection)_array).SyncRoot; }
        }

        #endregion

        #region IList{T} members

        T IList<T>.this[int index] { get => this[index]; set => throw new NotImplementedException(); }

        void IList<T>.Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ICollection{T} members

        bool ICollection<T>.IsReadOnly => true;

        void ICollection<T>.Add(T item)
        {
            throw new NotImplementedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotImplementedException();
        }

        bool ICollection<T>.Contains(T item) => _array.Contains(item);

        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _array.CopyTo(array, arrayIndex);

        bool ICollection<T>.Remove(T item)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IHasMemory members

        public bool TryGetMemory(out Memory<T> memory)
        {
            memory = Memory<T>.Empty;
            return false;
        }

        public bool TryGetMemory(out ReadOnlyMemory<T> memory)
        {
            memory = AsMemory();
            return true;
        }

        #endregion

        // used in integration
        public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int length)
        {
            _array.CopyTo(sourceIndex, destination, destinationIndex, length);
        }
    }

    /// <summary>
    /// Contains all those methods which are used in C# and VL.
    /// Methods which are only used in VL reside in SpreadNodes.
    /// </summary>
    public static class Spread
    {
        public static Spread<T> Create<T>(ImmutableArray<T> values)
        {
            if (values.Length > 0)
                return new Spread<T>(values);
            return Spread<T>.Empty;
        }

        public static Spread<T> Create<T>(params T[] items) => Create(ImmutableArray.Create(items));

        public static Spread<T> Create<T>(T[] items, int start, int length) => Create(ImmutableArray.Create(items, start, length));

        public static SpreadBuilder<T> CreateBuilder<T>() => Spread<T>.Empty.ToBuilder();

        public static SpreadBuilder<T> CreateBuilder<T>(int initialCapacity) => new SpreadBuilder<T>(initialCapacity);

        public static Spread<T> ToSpread<T>(this IEnumerable<T> source)
        {
            var spread = source as Spread<T>;
            if (spread != null)
                return spread;
            var builder = source as SpreadBuilder<T>;
            if (builder != null)
                return builder.ToSpread();
            var collection = source as IReadOnlyCollection<T>;
            if (collection != null && collection.Count == 0)
                return Spread<T>.Empty;
            return new Spread<T>(ImmutableArray.CreateRange(source));
        }

        public static Spread<T> ToSpread<T>(this ReadOnlyMemory<T> source)
        {
            var array = source.ToArray();
            return AsSpreadUnsafe(array);
        }

        public static SpreadBuilder<T> ToSpreadBuilder<T>(this IEnumerable<T> source)
        {
            var builder = new SpreadBuilder<T>();
            builder.AddRange(source);
            return builder;
        }

        public static bool Contains<T>(this Spread<T> source, T item) => source._array.Contains(item);

        public static T Aggregate<T>(this Spread<T> source, Func<T, T, T> func) => source._array.Aggregate(func);
        public static TAccumulate Aggregate<TAccumulate, T>(this Spread<T> source, TAccumulate seed, Func<TAccumulate, T, TAccumulate> func) => source._array.Aggregate(seed, func);
        public static TResult Aggregate<TAccumulate, TResult, T>(this Spread<T> source, TAccumulate seed, Func<TAccumulate, T, TAccumulate> func, Func<TAccumulate, TResult> resultSelector) => source._array.Aggregate(seed, func, resultSelector);
        public static bool All<T>(this Spread<T> source, Func<T, bool> predicate) => source._array.All(predicate);
        public static bool Any<T>(this Spread<T> source) => source._array.Any();
        public static bool Any<T>(this Spread<T> source, Func<T, bool> predicate) => source._array.Any(predicate);
        public static T ElementAt<T>(this Spread<T> source, int index) => source._array.ElementAt(index);
        public static T ElementAtOrDefault<T>(this Spread<T> source, int index) => source._array.ElementAtOrDefault(index);
        public static IEnumerable<TResult> Select<T, TResult>(this Spread<T> source, Func<T, TResult> selector) => source._array.Select(selector);
        public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this Spread<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector) => source._array.SelectMany(collectionSelector, resultSelector);
        public static bool SequenceEqual<TDerived, TBase>(this Spread<TBase> source, IEnumerable<TDerived> items, IEqualityComparer<TBase> comparer = null) where TDerived : TBase => source._array.SequenceEqual(items, comparer);
        public static IEnumerable<T> Where<T>(this Spread<T> source, Func<T, bool> predicate) => source._array.Where(predicate);
        public static T First<T>(this Spread<T> source) => source._array.First();
        public static T First<T>(this Spread<T> source, Func<T, bool> predicate) => source._array.First(predicate);
        public static T Last<T>(this Spread<T> source) => source._array.Last();
        public static T Last<T>(this Spread<T> source, Func<T, bool> predicate) => source._array.Last(predicate);
        public static T FirstOrDefault<T>(this Spread<T> source) => source._array.FirstOrDefault();
        public static T FirstOrDefault<T>(this Spread<T> source, Func<T, bool> predicate) => source._array.FirstOrDefault(predicate);
        public static T LastOrDefault<T>(this Spread<T> source) => source._array.LastOrDefault();
        public static T LastOrDefault<T>(this Spread<T> source, Func<T, bool> predicate) => source._array.LastOrDefault(predicate);

        public static int SizeInBytesInt<T>(this Spread<T> source) => (int)source.SizeInBytes();

        /// <summary>
        /// The size in bytes = element type size * count.
        /// Returns IntPtr * count for reference types.
        /// </summary>
        public static long SizeInBytes<T>(this Spread<T> source)
        {
            var type = typeof(T);
            if(type.IsValueType)
            {
                return Marshal.SizeOf(type) * source.Count;
            }
            else
            { 
                return IntPtr.Size * source.Count;
            }
        }

        /// <summary>
        /// Gets the internal data array of the spread, use at your own risk, i.e. never modify it.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] GetInternalArray<T>(this Spread<T> input)     
        {
            var immutableArray = input._array;
            return Unsafe.As<ImmutableArray<T>, T[]>(ref immutableArray);
        }

        /// <summary>
        /// Interprets the array as a spread. The array must not mutate after this call or the immutability of the spread is violated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Spread<T> AsSpreadUnsafe<T>(this T[] input)
        {
            if (input.Length > 0)
            {
                var immutableArray = Unsafe.As<T[], ImmutableArray<T>>(ref input);
                return new Spread<T>(immutableArray);
            }
            else
            {
                return Spread<T>.Empty;
            }
        }
    }
}
