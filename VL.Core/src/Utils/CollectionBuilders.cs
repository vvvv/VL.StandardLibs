using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using VL.Lib.Collections;

namespace VL.Core.Utils
{
    /// <summary>
    /// These builders are used by the target code for pin groups and ouput splicers.
    /// </summary>
    public static partial class CollectionBuilders
    {
        public static SpreadBuilder<TValue> GetBuilder<TValue>(Spread<TValue> collection, int capacity)
        {
            return new SpreadBuilder<TValue>(collection, capacity);
        }

        public static ArrayBuilder<TValue> GetBuilder<TValue>(TValue[] collection, int capacity)
        {
            return new ArrayBuilder<TValue>(collection, capacity);
        }

        public static ImmutableArrayBuilder<TValue> GetBuilder<TValue>(ImmutableArray<TValue> collection, int capacity)
        {
            return new ImmutableArrayBuilder<TValue>(collection, capacity);
        }

        public static ImmutableDictionaryBuilder<TKey, TValue> GetBuilder<TKey, TValue>(ImmutableDictionary<TKey, TValue> collection, int capacity)
        {
            return new ImmutableDictionaryBuilder<TKey, TValue>(collection);
        }

        public static DictionaryBuilder<TKey, TValue> GetBuilder<TKey, TValue>(Dictionary<TKey, TValue> collection, int capacity)
        {
            return new DictionaryBuilder<TKey, TValue>(collection, capacity);
        }
    }
}
