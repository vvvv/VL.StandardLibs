using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace VL.Lib.Collections
{
    public static partial class EnumerableNodes
    {
        const string C = "Collections.Sequence";

        /// <summary>
        /// Applies an accumulator function over a sequence. The specified seed value is used as the initial accumulator value
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TAccumulate"></typeparam>
        /// <param name="input"></param>
        /// <param name="seed"></param>
        /// <param name="accumulator"></param>
        /// <returns></returns>
        public static TAccumulate Aggregate<TSource, TAccumulate>(IEnumerable<TSource> input, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator) => Enumerable.Aggregate(input, seed, accumulator);

        // All is defined in VL.CoreLib

        // Any is defined in VL.CoreLib

        // Average is defined in VL.CoreLib.vl

        /// <summary>
        /// Casts the elements of an System.Collections.IEnumerable to the specified type
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static IEnumerable<TResult> Cast<TResult>(IEnumerable input) => Enumerable.Cast<TResult>(input);

        /// <summary>
        /// Concatenates all incoming sequences to one sequence
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> Concat<TSource>(IEnumerable<TSource> input, IEnumerable<TSource> input2) => Enumerable.Concat(input, input2);

        /// <summary>
        /// Returns the number of elements in a sequence
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static int Count<TSource>(IEnumerable<TSource> input) => Enumerable.Count(input);

        /// <summary>
        /// Returns a number that represents how many elements in the specified sequence satisfy a condition
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="input"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static int Count<TSource>(IEnumerable<TSource> input, Func<TSource, bool> predicate) => Enumerable.Count(input, predicate);

        /// <summary>
        /// Returns the elements of the specified sequence or the specified value in a singleton collection if the sequence is empty
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="input"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> DefaultIfEmpty<TSource>(IEnumerable<TSource> input, TSource defaultValue) => Enumerable.DefaultIfEmpty(input, defaultValue);

        /// <summary>
        /// Returns distinct elements from a sequence by using the default equality comparer to compare values
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> Distinct<TSource>(IEnumerable<TSource> input) => Enumerable.Distinct(input);

        /// <summary>
        /// Returns an empty sequence
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static IEnumerable<TResult> Empty<TResult>() => Enumerable.Empty<TResult>();

        /// <summary>
        /// Produces the set difference of two sequences by using the default equality comparer to compare values
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> Except<TSource>(IEnumerable<TSource> input, IEnumerable<TSource> input2) => Enumerable.Except(input, input2);

        /// <summary>
        /// Groups the elements of a sequence according to a specified key selector function
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="input"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(IEnumerable<TSource> input, Func<TSource, TKey> keySelector) => Enumerable.GroupBy(input, keySelector);

        /// <summary>
        /// Produces the set intersection of two sequences by using the default equality comparer to compare values
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> Intersect<TSource>(IEnumerable<TSource> input, IEnumerable<TSource> input2) => Enumerable.Intersect(input, input2);

        // Max is defined in VL.CoreLib.vl

        // Min is defined in VL.CoreLib.vl

        /// <summary>
        /// Filters the elements of a sequence based on a specified type
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static IEnumerable<TResult> OfType<TResult>(IEnumerable input)
        {
            return IHotswapSpecificNodes.Impl.OfType<TResult>(input);
        }

        /// <summary>
        /// Sorts the elements of a sequence in ascending order according to a key
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="input"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(IEnumerable<TSource> input, Func<TSource, TKey> keySelector) => Enumerable.OrderBy(input, keySelector);

        /// <summary>
        /// Sorts the elements of a sequence in descending order according to a key
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="input"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(IEnumerable<TSource> input, Func<TSource, TKey> keySelector) => Enumerable.OrderByDescending(input, keySelector);

        /// <summary>
        /// Inverts the order of the elements in a sequence
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> Reverse<TSource>(IEnumerable<TSource> input) => Enumerable.Reverse(input);

        /// <summary>
        /// Projects each element of a sequence into a new form by incorporating the element's index
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="input"></param>
        /// <param name="transformer"></param>
        /// <returns></returns>
        public static IEnumerable<TResult> Project<TSource, TResult>(IEnumerable<TSource> input, Func<TSource, int, TResult> transformer) => Enumerable.Select(input, transformer);

        /// <summary>
        /// Projects each element of a sequence to a sequence, and flattens the resulting sequences into one sequence. The index of each source element is used in the projected form of that element
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="input"></param>
        /// <param name="transformer"></param>
        /// <returns></returns>
        public static IEnumerable<TResult> Project<TSource, TResult>(IEnumerable<TSource> input, Func<TSource, int, IEnumerable<TResult>> transformer) => Enumerable.SelectMany(input, transformer);

        // SequenceEqual is defined in VL.CoreLib

        /// <summary>
        /// Bypasses a specified number of elements in a sequence and then returns the remaining elements
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="input"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> Skip<TSource>(IEnumerable<TSource> input, int count) => Enumerable.Skip(input, count);

        /// <summary>
        /// Bypasses elements in a sequence as long as a specified condition is true and then returns the remaining elements. The element's index is used in the logic of the predicate function
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="input"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> SkipWhile<TSource>(IEnumerable<TSource> input, Func<TSource, int, bool> predicate) => Enumerable.SkipWhile(input, predicate);

        /// <summary>
        /// Returns a specified number of contiguous elements from the start of a sequence
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="input"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> Take<TSource>(IEnumerable<TSource> input, int count) => Enumerable.Take(input, count);

        /// <summary>
        /// Returns elements from a sequence as long as a specified condition is true. The element's index is used in the logic of the predicate function
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="input"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> TakeWhile<TSource>(IEnumerable<TSource> input, Func<TSource, int, bool> predicate) => Enumerable.TakeWhile(input, predicate);

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in ascending order according to a key
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="input"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(IOrderedEnumerable<TSource> input, Func<TSource, TKey> keySelector) => Enumerable.ThenBy(input, keySelector);

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in descending order, according to a key
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="input"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(IOrderedEnumerable<TSource> input, Func<TSource, TKey> keySelector) => Enumerable.ThenByDescending(input, keySelector);

        /// <summary>
        /// Creates an array from a sequence
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static TSource[] ToArray<TSource>(IEnumerable<TSource> input) => Enumerable.ToArray(input);

        /// <summary>
        /// Produces the set union of two sequences by using the default equality comparer
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> Union<TSource>(IEnumerable<TSource> input, IEnumerable<TSource> input2) => Enumerable.Union(input, input2);

        /// <summary>
        /// Filters a sequence of values based on a predicate. Each element's index is used in the logic of the predicate function
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="input"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> Where<TSource>(IEnumerable<TSource> input, Func<TSource, int, bool> predicate) => Enumerable.Where(input, predicate);

        /// <summary>
        /// Applies a specified function to the corresponding elements of two sequences, producing a sequence of the results
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <param name="resultSelector"></param>
        /// <returns></returns>
        public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(IEnumerable<TFirst> input, IEnumerable<TSecond> input2, Func<TFirst, TSecond, TResult> resultSelector) => Enumerable.Zip(input, input2, resultSelector);
    }
}
