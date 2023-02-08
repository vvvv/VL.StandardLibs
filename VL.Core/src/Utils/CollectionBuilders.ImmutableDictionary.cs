using System.Collections.Generic;
using System.Collections.Immutable;

namespace VL.Core.Utils
{
    partial class CollectionBuilders
    {
        public struct ImmutableDictionaryBuilder<TKey, TValue>
        {
            readonly ImmutableDictionary<TKey, TValue>.Builder builder;
            readonly HashSetPool<TKey> pool;
            readonly HashSet<TKey> keys;

            public ImmutableDictionaryBuilder(ImmutableDictionary<TKey, TValue> dictionary)
            {
                this.builder = dictionary?.ToBuilder() ?? ImmutableDictionary.CreateBuilder<TKey, TValue>();
                if (dictionary != null)
                {
                    pool = HashSetPool<TKey>.GetPool(dictionary.KeyComparer);
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

            public ImmutableDictionary<TKey, TValue> Commit()
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
                return builder.ToImmutable();
            }
        }
    }
}
