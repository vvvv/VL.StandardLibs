using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace VL.Core.Utils
{
    public sealed class DictionaryPool<TKey, TValue> : DefaultObjectPool<Dictionary<TKey, TValue>>
    {
        class Policy : PooledObjectPolicy<Dictionary<TKey, TValue>>
        {
            public override Dictionary<TKey, TValue> Create() => new Dictionary<TKey, TValue>();

            public override bool Return(Dictionary<TKey, TValue> obj)
            {
                if (obj.Count <= 4096)
                {
                    obj.Clear();
                    return true;
                }
                return false;
            }
        }

        public static readonly DictionaryPool<TKey, TValue> Default = new DictionaryPool<TKey, TValue>();

        public DictionaryPool() : base(new Policy())
        {
        }

        public Dictionary<TKey, TValue> Rent() => Get();
    }

    public sealed class ListPool<TValue> : DefaultObjectPool<List<TValue>>
    {
        class Policy : PooledObjectPolicy<List<TValue>>
        {
            public override List<TValue> Create() => new List<TValue>();

            public override bool Return(List<TValue> obj)
            {
                obj.Clear();
                return true;
            }
        }

        public static readonly ListPool<TValue> Default = new ListPool<TValue>();

        public ListPool() : base(new Policy())
        {
        }

        public List<TValue> Rent() => Get();
    }

    public sealed class StackPool<TValue> : DefaultObjectPool<Stack<TValue>>
    {
        class Policy : PooledObjectPolicy<Stack<TValue>>
        {
            public override Stack<TValue> Create() => new Stack<TValue>();

            public override bool Return(Stack<TValue> obj)
            {
                obj.Clear();
                return true;
            }
        }

        public static readonly StackPool<TValue> Default = new StackPool<TValue>();

        public StackPool() : base(new Policy())
        {
        }

        public Stack<TValue> Rent() => Get();
    }

    public sealed class ImmutableArrayBuilderPool<TValue> : DefaultObjectPool<ImmutableArray<TValue>.Builder>
    {
        class Policy : PooledObjectPolicy<ImmutableArray<TValue>.Builder>
        {
            public override ImmutableArray<TValue>.Builder Create() => ImmutableArray.CreateBuilder<TValue>();

            public override bool Return(ImmutableArray<TValue>.Builder obj)
            {
                obj.Clear();
                return true;
            }
        }

        public static readonly ImmutableArrayBuilderPool<TValue> Default = new ImmutableArrayBuilderPool<TValue>();

        public ImmutableArrayBuilderPool() : base(new Policy())
        {
        }

        public ImmutableArray<TValue>.Builder Rent() => Get();
    }

    public sealed class ImmutableHashSetBuilderPool<TValue> : DefaultObjectPool<ImmutableHashSet<TValue>.Builder>
    {
        class Policy : PooledObjectPolicy<ImmutableHashSet<TValue>.Builder>
        {
            public override ImmutableHashSet<TValue>.Builder Create() => ImmutableHashSet.CreateBuilder<TValue>();

            public override bool Return(ImmutableHashSet<TValue>.Builder obj)
            {
                obj.Clear();
                return true;
            }
        }

        public static readonly ImmutableHashSetBuilderPool<TValue> Default = new ImmutableHashSetBuilderPool<TValue>();

        public ImmutableHashSetBuilderPool() : base(new Policy())
        {
        }

        public ImmutableHashSet<TValue>.Builder Rent() => Get();
    }

    public sealed class ImmutableDictionaryBuilderPool<TKey, TValue> : DefaultObjectPool<ImmutableDictionary<TKey, TValue>.Builder>
    {
        class Policy : PooledObjectPolicy<ImmutableDictionary<TKey, TValue>.Builder>
        {
            readonly IEqualityComparer<TKey> keyComparer;
            readonly IEqualityComparer<TValue> valueComparer;

            public Policy()
            {
                keyComparer = EqualityComparer<TKey>.Default;
                valueComparer = EqualityComparer<TValue>.Default;
            }

            public override ImmutableDictionary<TKey, TValue>.Builder Create() => ImmutableDictionary.CreateBuilder<TKey, TValue>();

            public override bool Return(ImmutableDictionary<TKey, TValue>.Builder obj)
            {
                obj.Clear();
                obj.KeyComparer = keyComparer;
                obj.ValueComparer = valueComparer;
                return true;
            }
        }

        public static readonly ImmutableDictionaryBuilderPool<TKey, TValue> Default = new ImmutableDictionaryBuilderPool<TKey, TValue>();

        public ImmutableDictionaryBuilderPool() : base(new Policy())
        {
        }

        public ImmutableDictionary<TKey, TValue>.Builder Rent() => Get();
    }

    public sealed class HashSetPool<TKey> : DefaultObjectPool<HashSet<TKey>>
    {
        class Policy : PooledObjectPolicy<HashSet<TKey>>
        {
            readonly IEqualityComparer<TKey> comparer;

            public Policy(IEqualityComparer<TKey> comparer)
            {
                this.comparer = comparer;
            }

            public override HashSet<TKey> Create() => new HashSet<TKey>();

            public override bool Return(HashSet<TKey> obj)
            {
                if (obj.Comparer != comparer)
                    throw new InvalidOperationException("The comparer of the returned hash set doesn't match the one from the pool.");

                obj.Clear();
                return true;
            }
        }

        public static readonly HashSetPool<TKey> Default = new HashSetPool<TKey>(new Policy(EqualityComparer<TKey>.Default));

        static readonly Dictionary<IEqualityComparer<TKey>, HashSetPool<TKey>> Pools = new Dictionary<IEqualityComparer<TKey>, HashSetPool<TKey>>();

        public static HashSetPool<TKey> GetPool(IEqualityComparer<TKey> comparer)
        {
            if (comparer == EqualityComparer<TKey>.Default)
                return Default;

            var pools = Pools;
            lock (pools)
            {
                if (!pools.TryGetValue(comparer, out HashSetPool<TKey> pool))
                    pools.Add(comparer, pool = new HashSetPool<TKey>(new Policy(comparer)));
                return pool;
            }
        }

        HashSetPool(Policy policy)
            : base(policy)
        {
        }

        public HashSet<TKey> Rent() => Get();
    }
}
