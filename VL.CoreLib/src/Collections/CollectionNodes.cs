using System;
using System.Collections.Generic;
using VL.Lib.Primitive;

namespace VL.Lib.Collections
{
    public static class CollectionNodes
    {
        /// <summary>
        /// Returns the item at the specified index or the given default value in case the collection is empty.
        /// If the index is out-of-bounds, it will be mapped back into the collection with a modulo operation, i.e. the collection behaves like a ring.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="defaultValue"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static T GetSlice<T>(this IReadOnlyList<T> input, T defaultValue, int index)
        {
            var inputCount = input.Count;
            if (inputCount == 0)
                return defaultValue;
            return input[index.ZMOD(inputCount)];
        }

        /// <summary>
        /// Returns a sub-list of the collection that is addressed via index and count.
        /// If the index is out-of-bounds, it will be mapped back into the collection with a modulo operation, i.e. the collection behaves like a ring.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IReadOnlyList<T> GetList<T>(this IReadOnlyList<T> input, int index, int count)
        {
            var inputCount = input.Count;
            if (inputCount == 0)
                return input;
            if (count <= 0)
                return Array.Empty<T>();
            index = index.ZMOD(inputCount);
            var list = new List<T>(count);
            while (list.Count < count)
                list.Add(input[index++ % inputCount]);
            return list;
        }

        /// <summary>
        /// Applies the selector for each pair in the collection and returns a new collection with one less item.
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="input"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IReadOnlyList<TOutput> Pairwise<TInput, TOutput>(this IReadOnlyList<TInput> input, Func<TInput, TInput, TOutput> selector)
        {
            var inputCount = input.Count;
            if (inputCount == 0)
                return Array.Empty<TOutput>();
            var list = new List<TOutput>(inputCount - 1);
            for (int i = 0; i < (inputCount - 1); i++)
                list.Add(selector(input[i], input[i + 1]));
            return list;
        }

        /// <summary>
        /// Replaces the item at the specified index with the new value.
        /// If the index is out-of-bounds, it will be mapped back into the collection with a modulo operation, i.e. the collection behaves like a ring.
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static TCollection SetSlice<TCollection, T>(this TCollection input, T value, int index) where TCollection : IList<T>
        {
            var inputCount = input.Count;
            if (inputCount == 0)
                return input;

            input[index.ZMOD(inputCount)] = value;
            return input;
        }    
    }
}
