using System;
using System.Collections.Generic;
using System.Linq;

namespace VL.Lib.Collections
{
    public static class Compare
    {
        /// <summary>
        /// Creates an equality comparer from a key selector delegate
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="selector">The selector.</param>
        public static IEqualityComparer<T> By<T, TKey>(Func<T, TKey> selector)
        {
            return new DelegateComparer<T, TKey>(selector);
        }

        private class DelegateComparer<T, TKey> : IEqualityComparer<T>
        {
            private readonly Func<T, TKey> identitySelector;

            public DelegateComparer(Func<T, TKey> identitySelector)
            {
                this.identitySelector = identitySelector;
            }

            public bool Equals(T x, T y)
            {
                return Equals(identitySelector(x), identitySelector(y));
            }

            public int GetHashCode(T obj)
            {
                return identitySelector(obj).GetHashCode();
            }
        }
    }
}
