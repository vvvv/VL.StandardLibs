using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using VL.Lib.Collections;

namespace VL.Core.Utils
{
    public static class LazyHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Cache<T>(ref T location, Func<T> query)
        {
            if (location == null)
                location = query();
            return location;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Spread<T> Cache<T>(ref Spread<T> location, Func<IEnumerable<T>> query)
        {
            if (location == null)
            {
                var value = query().ToSpread();
                var original = Interlocked.CompareExchange(ref location, value, null);
                if (original != null)
                    return original;
                return value;
            }
            return location;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Cache<T>(ref T? location, Func<T> query) where T : struct
        {
            if (location == null)
                location = query();
            return location.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Cache<T>(ref Lazy<T> location, Func<T> query)
        {
            if (location == null)
                location = new Lazy<T>(query, LazyThreadSafetyMode.PublicationOnly);
            return location.Value;
        }
    }
}
