using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using VL.Core;

namespace VL.Lib.Primitive
{
    public static class Integer32Extensions
    {
        public static readonly int One = 1;

        public static readonly int Zero = 0;

        /// <summary>
        /// Increments the input by 1
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Inc(this int input)
        {
            return input + 1;
        }

        /// <summary>
        /// Decrements the input by 1
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Dec(this int input)
        {
            return input - 1;
        }


        /// <summary>
        /// Modulo operator with the property, that the remainder of a division z / d
        /// and z &lt; 0 is positive. For example: zmod(-2, 30) = 28.
        /// </summary>
        /// <param name="z"></param>
        /// <param name="input2"></param>
        /// <returns>Remainder of division z / d.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ZMOD(this int z, int input2 = 1)
        {
            if (z >= input2)
                return z % input2;
            else if (z < 0)
            {
                var remainder = z % input2;
                return remainder == 0 ? 0 : remainder + input2;
            }
            else
                return z;
        }     

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Lerp(int input, int input2, float scalar)
        {
            return (int)(input + (input2 - input) * scalar);
        }
    }
}
