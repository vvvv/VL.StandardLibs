using System;
using System.Collections.Generic;
using System.Linq;
using VL.Core;

namespace VL.Lib.Collections.Spread
{
    public static class SpreadGenerators
    {
        /// <summary>
        /// Returns the input as a SpreadBuilder with one element
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static SpreadBuilder<T> ToSpreadBuilder<T>(T input)
        {
            var builder = new SpreadBuilder<T>(1);
            builder.Add(input);
            return builder;
        }

        public enum LinearSpreadAlignment { Centered, LeftJustified, RightJustified, Block }

        public static T LinearSpreadAlignmentSwitch<T>(LinearSpreadAlignment alignment, Func<T> centered, Func<T> left, Func<T> right, Func<T> block)
            => alignment switch
            {
                LinearSpreadAlignment.Centered => centered(),
                LinearSpreadAlignment.LeftJustified => left(),
                LinearSpreadAlignment.RightJustified => right(),
                LinearSpreadAlignment.Block => block(),
                _ => throw new NotImplementedException()
            };

        /// <summary>
        /// Creates a spread of random values
        /// </summary>
        /// <param name="center"></param>
        /// <param name="width"></param>
        /// <param name="seed"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Spread<float> RandomSpread(float center = 0.0f, float width = 1.0f, int seed = 0, int count = 1)
        {
            var random = new Random(seed);
            var builder = new SpreadBuilder<float>(System.Math.Max(count, 0));
            for (int i = 0; i < count; i++)
                builder.Add(center + (float)(random.NextDouble() - 0.5) * width);
            return builder.ToSpread();
        }

        /// <summary>
        /// Generates a spread that contains the same value repeated the given number of times
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Spread<T> Repeat<T>(T element, int count)
        {
            return EnumerableEx.Repeat(element, count)
                .ToSpread();
        }
    }
}

