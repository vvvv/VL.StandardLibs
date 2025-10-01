using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using System.Runtime.CompilerServices;

namespace VL.Lib.Primitive
{
    public static class Float64Extensions
    {

        public static readonly double One = 1;

        public static readonly double Zero = 0;

        /// <summary>
        /// Modulo operator with the property, that the remainder of a division z / d
        /// and z &lt; 0 is positive. For example: zmod(-2, 30) = 28.
        /// </summary>
        /// <param name="z"></param>
        /// <param name="input2"></param>
        /// <returns>Remainder of division z / d.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ZMOD(this double z, double input2 = 1d)
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
        public static double Lerp(double input, double input2, float scalar)
        {
            return input + (input2 - input) * scalar;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Round(this double input)
        {
            return input > 0 ? (int)(input + 0.5) : (int)(input - 0.5);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Frac(this double input, out int wholePart, out double fractionalPart)
        {
            wholePart = (int)input;
            fractionalPart = input - wholePart;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FracSawtooth(this double input, out int wholePart, out double fractionalPart)
        {
            wholePart = (int)input;
            fractionalPart = input - wholePart;

            if (input < 0)
            {
                wholePart--;
                fractionalPart++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FracFloat(this double input, out double wholePart, out double fractionalPart)
        {
            wholePart = (int)input;
            fractionalPart = input - wholePart;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FracFloatSawtooth(this double input, out double wholePart, out double fractionalPart)
        {
            wholePart = (int)input;
            fractionalPart = input - wholePart;

            if (input < 0)
            {
                wholePart--;
                fractionalPart++;
            }
        }

        //2x faster
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Floor(this double input)
        {
            var integer = (int)input;
            if (integer > input) integer--;
            return integer;
        }

        //2x faster
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Ceil(this double input)
        {
            var integer = (int)input;
            if (integer < input) integer++;
            return integer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double RoundFloat(this double input)
        {
            return input > 0 ? (int)(input + 0.5f) : (int)(input - 0.5f);
        }

        //2x faster
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double FloorFloat(this double input)
        {
            var integer = (int)input;
            if (integer > input) integer--;
            return integer;
        }

        //2x faster
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CeilFloat(this double input)
        {
            var integer = (int)input;
            if (integer < input) integer++;
            return integer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Length(this double input)
        {
            return (float)Math.Abs(input);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LengthSquared(this double input)
        {
            return (float)(input*input);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Normalize(this double input)
        {
            return Math.Sign(input);
        }

        public const double PI = System.Math.PI;
        public const double TwoPI = System.Math.PI * 2;
        internal const double CyclesToRadians64 = TwoPI;
        internal const double RadiansToCycles64 = 1d / TwoPI;
        internal const double DegreeToCycles64 = 1d / 360.0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CyclesToRadians(this double cycles)
        {
            return CyclesToRadians64 * cycles;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double RadiansToCycles(this double radian)
        {
            return RadiansToCycles64 * radian;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CyclesToDegree(this double degree)
        {
            return degree * 360;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DegreeToCycles(this double degree)
        {
            return degree * DegreeToCycles64;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Scale(double input, float scalar)
        {
            return input * scalar;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DivideScale(double input, float scalar = 1f)
        {
            return input / scalar;
        }

        /// <summary>
        /// Performs a Catmull-Rom interpolation using the specified positions.
        /// </summary>
        /// <param name="value1">The first position in the interpolation.</param>
        /// <param name="value2">The second position in the interpolation.</param>
        /// <param name="value3">The third position in the interpolation.</param>
        /// <param name="value4">The fourth position in the interpolation.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <returns>The result of the Catmull-Rom interpolation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CatmullRom(double value1, double value2, double value3, double value4, float amount)
        {
            double squared = amount * amount;
            double cubed = amount * squared;

            return 0.5f * ((((2.0f * value2) + ((-value1 + value3) * amount)) +
            (((((2.0f * value1) - (5.0f * value2)) + (4.0f * value3)) - value4) * squared)) +
            ((((-value1 + (3.0f * value2)) - (3.0f * value3)) + value4) * cubed));

        }

    }

    /// <summary>
    /// A class to allow the conversion of doubles to string representations of
    /// their exact decimal values. The implementation aims for readability over
    /// efficiency.
    /// </summary>
    public static class DoubleConverter
    {
        /// <summary>
        /// Converts the given double to a string representation of its
        /// exact decimal value.
        /// </summary>
        /// <param name="input">The double to convert.</param>
        /// <returns>A string representation of the double's exact decimal value.</returns>
        public static string ToExactString(double input)
        {
            if (double.IsPositiveInfinity(input))
                return "+Infinity";
            if (double.IsNegativeInfinity(input))
                return "-Infinity";
            if (double.IsNaN(input))
                return "NaN";

            // Translate the double into sign, exponent and mantissa.
            long bits = BitConverter.DoubleToInt64Bits(input);
            // Note that the shift is sign-extended, hence the test against -1 not 1
            bool negative = (bits < 0);
            int exponent = (int)((bits >> 52) & 0x7ffL);
            long mantissa = bits & 0xfffffffffffffL;

            // Subnormal numbers; exponent is effectively one higher,
            // but there's no extra normalisation bit in the mantissa
            if (exponent == 0)
            {
                exponent++;
            }
            // Normal numbers; leave exponent as it is but add extra
            // bit to the front of the mantissa
            else
            {
                mantissa = mantissa | (1L << 52);
            }

            // Bias the exponent. It's actually biased by 1023, but we're
            // treating the mantissa as m.0 rather than 0.m, so we need
            // to subtract another 52 from it.
            exponent -= 1075;

            if (mantissa == 0)
            {
                return "0";
            }

            /* Normalize */
            while ((mantissa & 1) == 0)
            {    /*  i.e., Mantissa is even */
                mantissa >>= 1;
                exponent++;
            }

            // Construct a new decimal expansion with the mantissa
            ArbitraryDecimal ad = new ArbitraryDecimal(mantissa);

            // If the exponent is less than 0, we need to repeatedly
            // divide by 2 - which is the equivalent of multiplying
            // by 5 and dividing by 10.
            if (exponent < 0)
            {
                for (int i = 0; i < -exponent; i++)
                    ad.MultiplyBy(5);
                ad.Shift(-exponent);
            }
            // Otherwise, we need to repeatedly multiply by 2
            else
            {
                for (int i = 0; i < exponent; i++)
                    ad.MultiplyBy(2);
            }

            // Finally, return the string with an appropriate sign
            if (negative)
                return "-" + ad.ToString();
            else
                return ad.ToString();
        }

        /// <summary>
        /// Private class used for manipulating
        /// </summary>
        class ArbitraryDecimal
        {
            /// <summary>
            /// Digits in the decimal expansion, one byte per digit
            /// </summary>
            byte[] digits;

            /// <summary> 
            /// How many digits are *after* the decimal point
            /// </summary>
            int decimalPoint = 0;

            /// <summary> 
            /// Constructs an arbitrary decimal expansion from the given long.
            /// The long must not be negative.
            /// </summary>
            internal ArbitraryDecimal(long x)
            {
                string tmp = x.ToString(CultureInfo.InvariantCulture);
                digits = new byte[tmp.Length];
                for (int i = 0; i < tmp.Length; i++)
                    digits[i] = (byte)(tmp[i] - '0');
                Normalize();
            }

            /// <summary>
            /// Multiplies the current expansion by the given amount, which should
            /// only be 2 or 5.
            /// </summary>
            internal void MultiplyBy(int amount)
            {
                byte[] result = new byte[digits.Length + 1];
                for (int i = digits.Length - 1; i >= 0; i--)
                {
                    int resultDigit = digits[i] * amount + result[i + 1];
                    result[i] = (byte)(resultDigit / 10);
                    result[i + 1] = (byte)(resultDigit % 10);
                }
                if (result[0] != 0)
                {
                    digits = result;
                }
                else
                {
                    Array.Copy(result, 1, digits, 0, digits.Length);
                }
                Normalize();
            }

            /// <summary>
            /// Shifts the decimal point; a negative value makes
            /// the decimal expansion bigger (as fewer digits come after the
            /// decimal place) and a positive value makes the decimal
            /// expansion smaller.
            /// </summary>
            internal void Shift(int amount)
            {
                decimalPoint += amount;
            }

            /// <summary>
            /// Removes leading/trailing zeroes from the expansion.
            /// </summary>
            internal void Normalize()
            {
                int first;
                for (first = 0; first < digits.Length; first++)
                    if (digits[first] != 0)
                        break;
                int last;
                for (last = digits.Length - 1; last >= 0; last--)
                    if (digits[last] != 0)
                        break;

                if (first == 0 && last == digits.Length - 1)
                    return;

                byte[] tmp = new byte[last - first + 1];
                for (int i = 0; i < tmp.Length; i++)
                    tmp[i] = digits[i + first];

                decimalPoint -= digits.Length - (last + 1);
                digits = tmp;
            }

            /// <summary>
            /// Converts the value to a proper decimal string representation.
            /// </summary>
            public override String ToString()
            {
                char[] digitString = new char[digits.Length];
                for (int i = 0; i < digits.Length; i++)
                    digitString[i] = (char)(digits[i] + '0');

                // Simplest case - nothing after the decimal point,
                // and last real digit is non-zero, eg value=35
                if (decimalPoint == 0)
                {
                    return new string(digitString);
                }

                // Fairly simple case - nothing after the decimal
                // point, but some 0s to add, eg value=350
                if (decimalPoint < 0)
                {
                    return new string(digitString) +
                           new string('0', -decimalPoint);
                }

                // Nothing before the decimal point, eg 0.035
                if (decimalPoint >= digitString.Length)
                {
                    return "0." +
                        new string('0', (decimalPoint - digitString.Length)) +
                        new string(digitString);
                }

                // Most complicated case - part of the string comes
                // before the decimal point, part comes after it,
                // eg 3.5
                return new string(digitString, 0,
                                   digitString.Length - decimalPoint) +
                    "." +
                    new string(digitString,
                                digitString.Length - decimalPoint,
                                decimalPoint);
            }
        }
    }
}
