using Collections.Pooled;
using System;
using System.Collections.Immutable;
using VL.Lib.Collections;

namespace VL.IO.Redis
{
    public static class PooledExtensinon
    {
        public static T[] ToArray<T>(this PooledSet<T> set)
        {
            if (set.Count == 0)
            {
                return Array.Empty<T>();
            }

            T[] arr = new T[set.Count];
            set.CopyTo(arr, 0);

            return arr;
        }

        public static Spread<T> ToSpread<T>(this PooledSet<T> set)
        {
            return Spread.Create(set.ToArray());
        }

        public static ImmutableHashSet<T> ToImmutable<T>(this PooledSet<T> set)
        {
            return ImmutableHashSet.Create<T>(set.ToArray());
        }


    }
}
