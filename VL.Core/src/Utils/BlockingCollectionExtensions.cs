using System;
using System.Collections.Concurrent;
using System.Threading;

namespace VL.Core.Utils
{
    public static class BlockingCollectionExtensions
    {
        public static bool TryAddSafe<T>(this BlockingCollection<T> collection, T item)
        {
            try
            {
                return collection.TryAdd(item);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryAddSafe<T>(this BlockingCollection<T> collection, T item, int millisecondsTimeout)
        {
            try
            {
                return collection.TryAdd(item, millisecondsTimeout);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryAddSafe<T>(this BlockingCollection<T> collection, T item, TimeSpan timeout)
        {
            try
            {
                return collection.TryAdd(item, timeout);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryAddSafe<T>(this BlockingCollection<T> collection, T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            try
            {
                return collection.TryAdd(item, millisecondsTimeout, cancellationToken);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
