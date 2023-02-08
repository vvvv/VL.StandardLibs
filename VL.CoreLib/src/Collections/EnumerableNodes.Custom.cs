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
        /// <summary>
        /// Produces the set intersection of two sequences by using a key selector delegate to compare values
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> IntersectByKey<TSource, TKey>(IEnumerable<TSource> input, IEnumerable<TSource> input2, Func<TSource, TKey> selector) 
            => Enumerable.Intersect(input, input2, Compare.By(selector));

        /// <summary>
        /// Produces the set union of two sequences by using a key selector delegate to compare values
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> UnionByKey<TSource, TKey>(IEnumerable<TSource> input, IEnumerable<TSource> input2, Func<TSource, TKey> selector)
            => Enumerable.Union(input, input2, Compare.By(selector));

        /// <summary>
        /// Produces the set difference of two sequences by using a key selector delegate to compare values
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> ExceptByKey<TSource, TKey>(IEnumerable<TSource> input, IEnumerable<TSource> input2, Func<TSource, TKey> selector)
            => Enumerable.Except(input, input2, Compare.By(selector));

        /// <summary>
        /// Splits the sequence at the given index into two sequences
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <param name="output"></param>
        /// <param name="output2"></param>
        public static void SplitAt<T>(this IEnumerable<T> input, int index, out IEnumerable<T> output, out IEnumerable<T> output2)
        {
            output = Enumerable.Take(input, index);
            output2 = Enumerable.Skip(input, index);
        }

        /// <summary>
        /// Returns the input as a Sequence with one element
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static IEnumerable<T> ToSequence<T>(T input)
        {
            yield return input;
        }

        /// <summary>
        /// The inverse of Zip. Interprets the input sequence as being interleaved and untangles it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="output2"></param>
        public static void UnZip<T>(this IEnumerable<T> input, out IEnumerable<T> output, out IEnumerable<T> output2)
        {
            output = Untangle(input, false);
            output2 = Untangle(input, true);
        }

        private static IEnumerable<T> Untangle<T>(IEnumerable<T> input, bool skipFirst)
        {
            var take = skipFirst;
            foreach (var item in input)
            {
                take = !take;
                if (take)
                    yield return item;
            }
        }

        /// <summary>
        /// Interleaves the two input sequences. To get the functionality as defined in LINQ use Zip (LINQ)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <param name="useMinCount"></param>
        /// <returns></returns>
        public static IEnumerable<T> ZipIt<T>(this IEnumerable<T> input, IEnumerable<T> input2, bool useMinCount = false) // Slightly different name to avoid conflict with LINQ
        {
            if (useMinCount)
                return ZipItMin(input, input2);
            return ZipItMax(input, input2);
        }

        private static IEnumerable<T> ZipItMin<T>(IEnumerable<T> input1, IEnumerable<T> input2)
        {
            using (var enumerator1 = input1.GetEnumerator())
            using (var enumerator2 = input2.GetEnumerator())
            {
                while (enumerator1.MoveNext() && enumerator2.MoveNext())
                {
                    yield return enumerator1.Current;
                    yield return enumerator2.Current;
                }
            }
        }

        private static IEnumerable<T> ZipItMax<T>(IEnumerable<T> input1, IEnumerable<T> input2)
        {
            var enumerator1 = input1.GetEnumerator();
            var enumerator2 = input2.GetEnumerator();
            try
            {
                if (!(enumerator1.MoveNext() && enumerator2.MoveNext()))
                    yield break;

                var fullyEnumerated1 = false;
                var fullyEnumerated2 = false;
                while (!fullyEnumerated1 || !fullyEnumerated2)
                {
                    yield return enumerator1.Current;
                    yield return enumerator2.Current;
                    if (!enumerator1.MoveNext())
                    {
                        fullyEnumerated1 = true;
                        enumerator1.Dispose();
                        enumerator1 = input1.GetEnumerator();
                        if (!enumerator1.MoveNext())
                            yield break;
                    }
                    if (!enumerator2.MoveNext())
                    {
                        fullyEnumerated2 = true;
                        enumerator2.Dispose();
                        enumerator2 = input2.GetEnumerator();
                        if (!enumerator2.MoveNext())
                            yield break;
                    }
                }
            }
            finally
            {
                enumerator1.Dispose();
                enumerator2.Dispose();
            }
        }
    }
}
