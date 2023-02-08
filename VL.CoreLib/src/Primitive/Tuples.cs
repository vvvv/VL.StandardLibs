using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace VL.Lib.Primitive.Tuple
{
    public static class TupleHelpers
    {
        /// <summary>
        /// Splits the specified 8 item tuple.
        /// </summary>
        /// <typeparam name="T1">The type of item 1.</typeparam>
        /// <typeparam name="T2">The type of item 2.</typeparam>
        /// <typeparam name="T3">The type of item 3.</typeparam>
        /// <typeparam name="T4">The type of item 4.</typeparam>
        /// <typeparam name="T5">The type of item 5.</typeparam>
        /// <typeparam name="T6">The type of item 6.</typeparam>
        /// <typeparam name="T7">The type of item 7.</typeparam>
        /// <typeparam name="TRest">The type of item 8 or the rest tuple.</typeparam>
        /// <param name="input">The input.</param>
        /// <param name="item1">The item1.</param>
        /// <param name="item2">The item2.</param>
        /// <param name="item3">The item3.</param>
        /// <param name="item4">The item4.</param>
        /// <param name="item5">The item5.</param>
        /// <param name="item6">The item6.</param>
        /// <param name="item7">The item7.</param>
        /// <param name="rest">The item8 or the rest tuple.</param>
        public static void Split<T1, T2, T3, T4, T5, T6, T7, TRest>(this Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> input, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out TRest rest)
        {
            item1 = input.Item1;
            item2 = input.Item2;
            item3 = input.Item3;
            item4 = input.Item4;
            item5 = input.Item5;
            item6 = input.Item6;
            item7 = input.Item7;
            rest = input.Rest;
        }
    }
}
