using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace VL.Core.Utils
{
    public readonly struct Pooled<T> : IDisposable
        where T : class
    {
        public readonly T Value;
        readonly ObjectPool<T> Pool;

        public Pooled(T value, ObjectPool<T> pool)
        {
            Value = value;
            Pool = pool;
        }

        public void Free()
        {
            Pool.Return(Value);
        }

        public void Dispose()
        {
            Free();
        }

        public bool IsDefault => Pool == default;
    }

    public static class Pooled
    {
        public static Pooled<T> GetAsPooled<T>(this ObjectPool<T> pool) where T : class
        {
            return new Pooled<T>(pool.Get(), pool);
        }

        public static Pooled<T> AsPooled<T>(this T value, ObjectPool<T> pool) where T : class
        {
            return new Pooled<T>(value, pool);
        }

        public static Pooled<ImmutableArray<T>.Builder> GetArrayBuilder<T>()
        {
            return ImmutableArrayBuilderPool<T>.Default.GetAsPooled();
        }

        public static Pooled<ImmutableDictionary<TKey, TValue>.Builder> GetDictionaryBuilder<TKey, TValue>()
        {
            return ImmutableDictionaryBuilderPool<TKey, TValue>.Default.GetAsPooled();
        }

        public static Pooled<ImmutableHashSet<T>.Builder> GetHashSetBuilder<T>()
        {
            return ImmutableHashSetBuilderPool<T>.Default.GetAsPooled();
        }

        public static Pooled<HashSet<T>> GetHashSet<T>()
        {
            return HashSetPool<T>.Default.GetAsPooled();
        }

        public static Pooled<Stack<T>> GetStack<T>()
        {
            return StackPool<T>.Default.GetAsPooled();
        }

        public static Pooled<List<T>> GetList<T>()
        {
            return ListPool<T>.Default.GetAsPooled();
        }

        public static Pooled<Dictionary<TKey, TValue>> GetDictionary<TKey, TValue>()
        {
            return DictionaryPool<TKey, TValue>.Default.GetAsPooled();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<TKey, TValue>(this Pooled<Dictionary<TKey, TValue>> dict, TKey key, TValue value)
        {
            dict.Value.Add(key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<TKey, TValue>(this Pooled<Dictionary<TKey, TValue>> dict)
        {
            dict.Value.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue<TKey, TValue>(this Pooled<Dictionary<TKey, TValue>> dict, TKey key, out TValue value)
        {
            return dict.Value.TryGetValue(key, out value);
        }

        public static Pooled<List<T>> ToPooledList<T>(this IEnumerable<T> source)
        {
            var pooledList = GetList<T>();
            var set = pooledList.Value;
            foreach (var item in source)
                set.Add(item);
            return pooledList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<T>(this Pooled<List<T>> list, T item)
        {
            list.Value.Add(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<T>(this Pooled<List<T>> list)
        {
            list.Value.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Add<T>(this Pooled<HashSet<T>> set, T item)
        {
            return set.Value.Add(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Any<T>(this Pooled<HashSet<T>> set)
        {
            return set.Value.Count > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>(this Pooled<HashSet<T>> set, T item)
        {
            return set.Value.Contains(item);
        }

        public static Pooled<HashSet<T>> ToPooledHashSet<T>(this IEnumerable<T> source)
        {
            var pooledSet = GetHashSet<T>();
            var set = pooledSet.Value;
            foreach (var item in source)
                set.Add(item);
            return pooledSet;
        }

        public static Pooled<ImmutableArray<T>.Builder> ToArrayBuilder<T>(this IEnumerable<T> source)
        {
            var pool = ImmutableArrayBuilderPool<T>.Default;
            var builder = pool.Get();
            builder.AddRange(source);
            return builder.AsPooled(pool);
        }

        public static ImmutableArray<T> ToImmutableAndFree<T>(this Pooled<ImmutableArray<T>.Builder> pooledBuilder)
        {
            var builder = pooledBuilder.Value;
            if (builder.Count == builder.Capacity)
                return builder.MoveToImmutable();

            var @array = builder.ToImmutable();
            pooledBuilder.Free();
            return array;
        }

        public static void Add<T>(this Pooled<ImmutableArray<T>.Builder> builder, T item)
        {
            builder.Value.Add(item);
        }

        public static ImmutableDictionary<TKey, TValue> ToImmutableAndFree<TKey, TValue>(this Pooled<ImmutableDictionary<TKey, TValue>.Builder> pooledBuilder)
        {
            var builder = pooledBuilder.Value;
            var dictionary = builder.ToImmutable();
            pooledBuilder.Free();
            return dictionary;
        }

        public static ImmutableArray<T> ToImmutableArrayAndFree<T>(this Pooled<HashSet<T>> pooledSet)
        {
            var array = pooledSet.Value.ToImmutableArray();
            pooledSet.Free();
            return array;
        }

        public static ImmutableHashSet<T> ToImmutableAndFree<T>(this Pooled<ImmutableHashSet<T>.Builder> pooledSetBuilder)
        {
            var set = pooledSetBuilder.Value.ToImmutable();
            pooledSetBuilder.Free();
            return set;
        }
    }
}
