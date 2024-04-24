using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace VL.Lib.Collections
{
    /// <summary>
    /// All the nodes which are safe to use on a sequence. All the other are defined in the VL.DevLib project.
    /// </summary>
    public static partial class EnumerableNodes //some inputs may be sequences. if all is spread go to spread nodes
    {
        // Note: This method is used in Forwarding_MappingOfTypeParametersTest - so be aware of changing the type parameter order!
        /// <summary>
        /// Returns the first element of a sequence, or the given default value if the sequence contains no elements
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TSequence"></typeparam>
        /// <param name="input"></param>
        /// <param name="defaultValue"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static TSequence FirstOrDefault<T, TSequence>(TSequence input, T defaultValue, out T result)
            where TSequence : IEnumerable<T>
        {
            result = input.FirstOrDefault(defaultValue);
            return input;
        }

        /// <summary>
        /// Returns the first element of the sequence that satisfies a condition or the given default value if no such element is found
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TSequence"></typeparam>
        /// <param name="input"></param>
        /// <param name="defaultValue"></param>
        /// <param name="predicate"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static TSequence FirstOrDefault<T, TSequence>(TSequence input, T defaultValue, Func<T, bool> predicate, out T result)
            where TSequence : IEnumerable<T>
        {
            result = input.FirstOrDefault(predicate, defaultValue);
            return input;
        }

        public static void GetPairedSpreadsSwapped<T>(this IEnumerable<T> input, out Spread<T> ouput, out Spread<T> output2)
        {
            ouput = input.Skip(1).ToSpread();
            output2 = input.SkipLast(1).ToSpread();
        }

        public static void GetPairedSpreads<T>(this IEnumerable<T> input, out Spread<T> output, out Spread<T> output2)
        {
            output = input.SkipLast(1).ToSpread();
            output2 = input.Skip(1).ToSpread();
        }

        /// <summary>
        /// Returns the last element of a sequence, or the given default value if the sequence contains no elements
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TSequence"></typeparam>
        /// <param name="input"></param>
        /// <param name="defaultValue"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static TSequence LastOrDefault<T, TSequence>(TSequence input, T defaultValue, out T result)
            where TSequence : IEnumerable<T>
        {
            result = defaultValue;
            using (var enumerator = input.GetEnumerator())
            {
                while (enumerator.MoveNext())
                    result = enumerator.Current;
            }
            return input;
        }

        /// <summary>
        /// Returns the last element of a sequence that satisfies a condition or the given default value if no such element is found
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TSequence"></typeparam>
        /// <param name="input"></param>
        /// <param name="defaultValue"></param>
        /// <param name="predicate"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static TSequence LastOrDefault<T, TSequence>(TSequence input, T defaultValue, Func<T, bool> predicate, out T result)
            where TSequence : IEnumerable<T>
        {
            result = defaultValue;
            using (var enumerator = input.GetEnumerator())
            {
                while (enumerator.MoveNext())
                    if (predicate(enumerator.Current))
                        result = enumerator.Current;
            }
            return input;
        }

        /// <summary>
        /// Determines whether all elements of a bool sequence are true
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool AND(this IEnumerable<bool> input)
        {
            return Enumerable.All(input, b => b);
        }

        /// <summary>
        /// Determines whether any elements of a bool sequence is true
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool OR(this IEnumerable<bool> input)
        {
            return Enumerable.Any(input, b => b);
        }

        /// <summary>
        /// Determines whether all elements of a sequence satisfy a condition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static bool All<T>(IEnumerable<T> input, Func<T, bool> predicate)
        {
            return Enumerable.All(input, predicate);
        }

        /// <summary>
        /// Determines whether a sequence contains any elements
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool Any<T>(IEnumerable<T> input)
        {
            return Enumerable.Any(input);
        }

        /// <summary>
        /// Determines whether any element of a sequence satisfies a condition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static bool Any<T>(IEnumerable<T> input, Func<T, bool> predicate)
        {
            return Enumerable.Any(input, predicate);
        }

        /// <summary>
        /// Determines whether a sequence contains no elements
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool None<T>(IEnumerable<T> input)
        {
            return !input.Any();
        }

        /// <summary>
        /// Determines whether all elements of a sequence do not satisfy a condition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static bool None<T>(IEnumerable<T> input, Func<T, bool> predicate)
        {
            return !input.Any(predicate);
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements by using the default equality comparer for their type
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="input"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static bool SequenceEqual<TSource>(IEnumerable<TSource> input, IEnumerable<TSource> second)
        {
            return Enumerable.SequenceEqual(input, second);
        }

        /// <summary>
        /// Splits the sequence into its first element and the rest
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="defaultValue"></param>
        /// <param name="output"></param>
        /// <param name="result"></param>
        public static void SplitFirst<T>(this IEnumerable<T> input, T defaultValue, out IEnumerable<T> output, out T result)
        {
            output = FirstOrDefault(input, defaultValue, out result).Skip(1);
        }

        /// <summary>
        /// Splits the sequence into its last element and the rest
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="defaultValue"></param>
        /// <param name="output"></param>
        /// <param name="result"></param>
        public static void SplitLast<T>(this IEnumerable<T> input, T defaultValue, out IEnumerable<T> output, out T result)
        {
            output = LastOrDefault(input, defaultValue, out result).SkipLast(1);
        }

        public static string ToString(IEnumerable<char> input)
        {
            return new string(input.ToArray());
        }
    }
}
