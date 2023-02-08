using System.Collections.Generic;

namespace VL.Core.Utils
{
    partial class CollectionBuilders
    {
        public struct DictionaryBuilder<TKey, TValue>
        {
            readonly Dictionary<TKey, TValue> builder;
            readonly HashSetPool<TKey> pool;
            readonly HashSet<TKey> keys;

            public DictionaryBuilder(Dictionary<TKey, TValue> dictionary, int capacity)
            {
                this.builder = dictionary ?? new Dictionary<TKey, TValue>(capacity);
                if (dictionary != null)
                {
                    pool = HashSetPool<TKey>.GetPool(dictionary.Comparer);
                    var keys = this.keys = pool.Rent();
                    foreach (var key in dictionary.Keys)
                        keys.Add(key);
                }
                else
                {
                    pool = null;
                    keys = null;
                }
            }

            public void Add(TKey key, TValue value)
            {
                keys?.Remove(key);
                builder[key] = value;
            }

            public Dictionary<TKey, TValue> Commit()
            {
                if (keys != null)
                {
                    try
                    {
                        if (keys.Count > 0)
                        {
                            foreach (var key in keys)
                                builder.Remove(key);
                        }
                    }
                    finally
                    {
                        pool.Return(keys);
                    }
                }
                return builder;
            }
        }
    }
}
