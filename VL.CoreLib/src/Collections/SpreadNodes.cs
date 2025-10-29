using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using VL.Lib.Primitive;

namespace VL.Lib.Collections
{
    public static class SpreadNodes //if one or many input collections are sequences -> put a (Version = "Sequence") to it
    {
        /// <summary>
        /// Returns a new spread with the item added at the end
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Spread<T> Add<T>(this Spread<T> input, T item)
        {
            var builder = input.ToBuilder();
            builder.Add(item);
            return builder.ToSpread();
        }

        /// <summary>
        /// Adds the specified items to the end of the spread
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Spread<T> AddRange<T>(this Spread<T> input, IEnumerable<T> items)
        {
            var collection = items as ICollection;
            if (collection != null && collection.Count == 0)
                return input;
            var builder = input.ToBuilder();
            builder.AddRange(items);
            return builder.ToSpread();
        }

        /// <summary>
        /// Concatenates all incoming spreads to one spread
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Spread<T> Concat<T>(Spread<IEnumerable<T>> input)
        {
            var builder = new SpreadBuilder<T>();
            foreach (var s in input)
                builder.AddRange(s);
            return builder.ToSpread();
        }

        /// <summary>
        /// Returns the empty spread
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Spread<T> Clear<T>(this Spread<T> input)
        {
            return Spread<T>.Empty;
        }

        /// <summary>
        /// Returns distinct elements from a spread by using the default equality comparer to compare values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Spread<T> Distinct<T>(this Spread<T> input)
        {
            if (input.Count == 0)
                return input;
            return Enumerable.Distinct(input)
                .ToSpread();
        }

        /// <summary>
        /// Produces the set difference of two spreads by using the default equality comparer to compare values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static Spread<T> Except<T>(this Spread<T> input, Spread<T> input2)
        {
            if (input.Count == 0 && input2.Count == 0)
                return Spread<T>.Empty;
            return Enumerable.Except(input, input2)
                .ToSpread();
        }

        /// <summary>
        /// Returns the slice at the specified index or the given default value in case the spread is empty
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="defaultValue"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static T GetSlice<T>(this Spread<T> input, T defaultValue, int index)
        {
            var count = input.Count;
            if (count == 0)
                return defaultValue;
            return input[index.ZMOD(count)];
        }

        /// <summary>
        /// Returns a sub-spread of the spread that is addressed via index and count
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Spread<T> GetSpread<T>(this Spread<T> input, int index, int count)
        {
            if (input.Count == 0)
                return input;
            if (count <= 0)
                return Spread<T>.Empty;
            index = index.ZMOD(input.Count);
            var builder = new SpreadBuilder<T>(count);
            while (builder.Count < count)
                builder.Add(input[index++ % input.Count]);
            return builder.ToSpread();
        }

        /// <summary>
        /// Inserts the item at the specified index. Can throw ArgumentOutOfRangeException exception
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Spread<T> Insert<T>(this Spread<T> input, int index, T item)
        {
            var builder = input.ToBuilder();
            builder.Insert(index, item);
            return builder.ToSpread();
        }

        /// <summary>
        /// Inserts a new slice at the specified index
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Spread<T> InsertSlice<T>(this Spread<T> input, int index, T value)
        {
            var builder = input.ToBuilder();
            builder.Insert(index.ZMOD(input.Count + 1), value);
            return builder.ToSpread();
        }

        /// <summary>
        /// Applies the selector for each pair in the spread and returns a new spread with one less item
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="input"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static Spread<TOutput> Pairwise<TInput, TOutput>(this Spread<TInput> input, Func<TInput, TInput, TOutput> selector)
        {
            if (input.Count == 0)
                return Spread<TOutput>.Empty;
            var builder = new SpreadBuilder<TOutput>(input.Count - 1);
            for (int i = 0; i < (input.Count - 1); i++)
                builder.Add(selector(input[i], input[i + 1]));
            return builder.ToSpread();
        }

        /// <summary>
        /// Projects each element of a spread to a sequence, and flattens the resulting sequences into one spread
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="input"></param>
        /// <param name="transformer"></param>
        /// <returns></returns>
        public static Spread<TOutput> Project<TInput, TOutput>(this Spread<TInput> input, Func<TInput, IEnumerable<TOutput>> transformer)
        {
            if (input.Count == 0)
                return Spread<TOutput>.Empty;
            var builder = new SpreadBuilder<TOutput>();
            foreach (var item in input)
                foreach (var subItem in transformer(item))
                    builder.Add(subItem);
            return builder.ToSpread();
        }

        /// <summary>
        /// Removes the item at the specified index. Can throw ArgumentOutOfRangeException exception
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Spread<T> RemoveAt<T>(this Spread<T> input, int index)
        {
            var builder = input.ToBuilder();
            builder.RemoveAt(index);
            return builder.ToSpread();
        }

        /// <summary>
        /// Removes all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="match"></param>
        /// <returns></returns>
        public static Spread<T> RemoveAll<T>(this Spread<T> input, Func<T, bool> match)
        {
            var builder = input.ToBuilder();
            builder.RemoveAll(match);
            return builder.ToSpread();
        }

        /// <summary>
        /// Removes the slice at the specified index
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Spread<T> RemoveSliceAt<T>(this Spread<T> input, int index)
        {
            var count = input.Count;
            if (count == 0)
                return input;
            var builder = input.ToBuilder();
            builder.RemoveAt(index.ZMOD(count));
            return builder.ToSpread();
        }

        /// <summary>
        /// Returns a new spread of the given size using the item factory to create new items
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="size"></param>
        /// <param name="itemFactory"></param>
        /// <returns></returns>
        public static Spread<T> Resize<T>(this Spread<T> input, int size, Func<T> itemFactory)
        {
            if (size <= 0)
                return Spread<T>.Empty;
            if (input.Count != size)
            {
                var builder = new SpreadBuilder<T>(size);
                var enumerator = input.GetEnumerator();
                var i = -1;
                while (++i < size && enumerator.MoveNext())
                    builder.Add(enumerator.Current);
                for (var j = i; j < size; j++)
                    builder.Add(itemFactory());
                return builder.ToSpread();
            }
            return input;
        }

        /// <summary>
        /// Inverts the order of the elements in a spread
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Spread<T> Reverse<T>(this Spread<T> input)
        {
            if (input.Count <= 1)
                return input;
            var builder = new SpreadBuilder<T>(input.Count);
            for (int i = input.Count - 1; i >= 0; i--)
                builder.Add(input[i]);
            return builder.ToSpread();
        }

        /// <summary>
        /// Sorts the elements in the entire spread using the default comparer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Spread<T> Sort<T>(this Spread<T> input)
        {
            if (input.Count <= 1)
                return input;
            var builder = input.ToBuilder();
            builder.Sort();
            return builder.ToSpread();
        }

        /// <summary>
        /// Sorts the elements in the entire spread using the specified comparer function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static Spread<T> Sort<T>(this Spread<T> input, Func<T, T, int> comparer)
        {
            if (input.Count <= 1)
                return input;
            var builder = input.ToBuilder();
            builder.Sort(comparer);
            return builder.ToSpread();
        }

        /// <summary>
        /// Sorts the elements in the entire spread according to a key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="input"></param>
        /// <param name="descending"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public static Spread<T> OrderBy<T, TKey>(this Spread<T> input, bool descending, Func<T, TKey> keySelector)
        {
            if (input.Count <= 1)
                return input;
            var builder = input.ToBuilder();
            if (descending)
                return builder.OrderByDescending(keySelector).ToSpread();
            else
                return builder.OrderBy(keySelector).ToSpread();
        }

        /// <summary>
        /// Replaces the item at the specified index with the new value. Can throw ArgumentOutOfRangeException exception
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Spread<T> SetItem<T>(this Spread<T> input, int index, T item)
        {
            if (EqualityComparer<T>.Default.Equals(input[index], item))
                return input;
            var builder = input.ToBuilder();
            builder[index] = item;
            return builder.ToSpread();
        }

        /// <summary>
        /// Replaces the slice at the specified index with the new value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Spread<T> SetSlice<T>(this Spread<T> input, T value, int index)
        {
            if (input.Count == 0)
                return Spread<T>.Empty;
            index = index.ZMOD(input.Count);
            if (EqualityComparer<T>.Default.Equals(input[index], value))
                return input;
            var builder = input.ToBuilder();
            builder[index] = value;
            return builder.ToSpread();
        }

        //Replaces a sub-spread in the spread that is addressed via index and count with the new spread
        // TODO: Anyone? The SetSpread as known in vvvv gives me a headache ;)
        //public static Spread<T> SetSpread<T>(this Spread<T> input, Spread<T> input2, int index, int count)
        //{
        //    if (input.Count == 0)
        //        return Spread<T>.Empty;
        //    if (input2.Count == 0)
        //        return input;
        //}


        /// <summary>
        /// Bypasses a specified number of elements in the spread and then returns the remaining elements
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Spread<T> Skip<T>(this Spread<T> input, int count)
        {
            if (count <= 0)
                return input;
            if (count >= input.Count)
                return Spread<T>.Empty;
            return Enumerable.Skip(input, count)
                .ToSpread();
        }

        /// <summary>
        /// Bypasses a specified number of contiguous elements from the end of the spread and returns the remaining elements
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Spread<T> SkipLast<T>(this Spread<T> input, int count)
        {
            if (count <= 0)
                return input;
            if (count >= input.Count)
                return Spread<T>.Empty;
            return Enumerable.SkipLast(input, count)
                .ToSpread();
        }

        /// <summary>
        /// Splits the spread at the given index into two spreads
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <param name="output"></param>
        /// <param name="output2"></param>
        public static void SplitAt<T>(this Spread<T> input, int index, out Spread<T> output, out Spread<T> output2)
        {
            if (input.Count == 0)
            {
                output = Spread<T>.Empty;
                output2 = Spread<T>.Empty;
                return;
            }
            if (index <= 0)
            {
                output = Spread<T>.Empty;
                output2 = input;
                return;
            }
            if (index >= input.Count)
            {
                output = input;
                output2 = Spread<T>.Empty;
                return;
            }
            var builder1 = new SpreadBuilder<T>(index);
            var builder2 = new SpreadBuilder<T>(input.Count - index);
            var i = 0;
            foreach (var item in input)
            {
                if (i++ < index)
                    builder1.Add(item);
                else
                    builder2.Add(item);
            }
            output = builder1.ToSpread();
            output2 = builder2.ToSpread();
        }

        /// <summary>
        /// Splits the spread into its first slice and the rest
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="defaultValue"></param>
        /// <param name="output"></param>
        /// <param name="result"></param>
        public static void SplitFirst<T>(this Spread<T> input, T defaultValue, out Spread<T> output, out T result)
        {
            if (input.Count > 0)
            {
                result = input[0];
                output = input.RemoveAt(0);
            }
            else
            {
                result = defaultValue;
                output = Spread<T>.Empty;
            }
        }

        /// <summary>
        /// Splits the spread into its last slice and the rest
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="defaultValue"></param>
        /// <param name="output"></param>
        /// <param name="result"></param>
        public static void SplitLast<T>(this Spread<T> input, T defaultValue, out Spread<T> output, out T result)
        {
            var lastIndex = input.Count - 1;
            if (lastIndex >= 0)
            {
                result = input[lastIndex];
                output = input.RemoveAt(lastIndex);
            }
            else
            {
                result = defaultValue;
                output = Spread<T>.Empty;
            }
        }

        /// <summary>
        /// Returns a specified number of contiguous elements from the start of the spread
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Spread<T> Take<T>(this Spread<T> input, int count)
        {
            if (count <= 0)
                return Spread<T>.Empty;
            if (count >= input.Count)
                return input;
            return Enumerable.Take(input, count)
                .ToSpread();
        }

        /// <summary>
        /// Returns a specified number of contiguous elements from the end of the spread
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Spread<T> TakeLast<T>(this Spread<T> input, int count)
        {
            if (count <= 0)
                return Spread<T>.Empty;
            if (count >= input.Count)
                return input;
            return Enumerable.TakeLast(input, count)
                .ToSpread();
        }

        /// <summary>
        /// Produces the set union of two spreads by using the default equality comparer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static Spread<T> Union<T>(this Spread<T> input, Spread<T> input2)
        {
            if (input.Count == 0 && input2.Count == 0)
                return Spread<T>.Empty;
            return Enumerable.Union(input, input2)
                .ToSpread();
        }

        /// <summary>
        /// The inverse of Zip. Interprets the input sequence as being interleaved and untangles it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="output2"></param>
        public static void UnZip<T>(this Spread<T> input, out Spread<T> output, out Spread<T> output2)
        {
            if (input.Count == 0)
            {
                output = input;
                output2 = input;
                return;
            }
            var builder1 = new SpreadBuilder<T>(input.Count / 2);
            var builder2 = new SpreadBuilder<T>(input.Count / 2);
            var builder = builder1;
            foreach (var item in input)
            {
                builder.Add(item);
                if (builder == builder1)
                    builder = builder2;
                else
                    builder = builder1;
            }
            output = builder1.ToSpread();
            output2 = builder2.ToSpread();
        }

        /// <summary>
        /// Filters a spread based on a predicate. Each element's index is used in the logic of the predicate function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static Spread<T> Where<T>(this Spread<T> input, Func<T, int, bool> predicate)
        {
            if (input.Count == 0)
                return input;
            var builder = new SpreadBuilder<T>();
            var i = 0;
            foreach (var item in input)
                if (predicate(item, i++))
                    builder.Add(item);
            return builder.ToSpread();
        }

        /// <summary>
        /// Interleaves the two input spreads
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <param name="useMinCount"></param>
        /// <returns></returns>
        public static Spread<T> Zip<T>(this Spread<T> input, Spread<T> input2, bool useMinCount = false)
        {
            var count1 = input.Count;
            var count2 = input2.Count;
            if (count1 == 0)
                return input2;
            if (count2 == 0)
                return input;
            if (useMinCount || count1 == count2)
                return ZipMin(input, input2, count1, count2);
            return ZipMax(input, input2, count1, count2);
        }

        private static Spread<T> ZipMin<T>(Spread<T> input1, Spread<T> input2, int count1, int count2)
        {
            var builder = new SpreadBuilder<T>(Math.Min(count1, count2) * 2);
            var enumerator1 = input1.GetEnumerator();
            var enumerator2 = input2.GetEnumerator();
            while (enumerator1.MoveNext() && enumerator2.MoveNext())
            {
                builder.Add(enumerator1.Current);
                builder.Add(enumerator2.Current);
            }
            return builder.ToSpread();
        }

        private static Spread<T> ZipMax<T>(Spread<T> input1, Spread<T> input2, int count1, int count2)
        {
            var maxCount = Math.Max(count1, count2);
            var builder = new SpreadBuilder<T>(maxCount * 2);
            for (int i = 0; i < maxCount; i++)
            {
                builder.Add(input1[i % count1]);
                builder.Add(input2[i % count2]);
            }
            return builder.ToSpread();
        }
    }
}
