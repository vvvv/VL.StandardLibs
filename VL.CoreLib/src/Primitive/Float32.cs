using System;
using System.Collections.Generic;
using System.Linq;
using VL.Core;

namespace VL.Lib.Primitive
{
    public static class Float32Extensions
    {
        public static readonly float One = 1;

        public static readonly float Zero = 0;

        /// <summary>
        /// Modulo operator with the property, that the remainder of a division z / d
        /// and z &lt; 0 is positive. For example: zmod(-2, 30) = 28.
        /// </summary>
        /// <param name="z"></param>
        /// <param name="input2"></param>
        /// <returns>Remainder of division z / d.</returns>
        public static float ZMOD(this float z, float input2 = 1f)
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

        public static float Lerp(float input, float input2, float scalar)
        {
            return input + (input2 - input) * scalar;
        }

        //float version not in System.Math
        public static float Sin(this float input)
        {
            return (float)System.Math.Sin(input);
        }

        //float version not in System.Math
        public static float Cos(this float input)
        {
            return (float)System.Math.Cos(input);
        }

        //float version not in System.Math
        public static float Tan(this float input)
        {
            return (float)System.Math.Tan(input);
        }

        //float version not in System.Math
        public static float Asin(this float input)
        {
            return (float)System.Math.Asin(input);
        }

        //float version not in System.Math
        public static float Acos(this float input)
        {
            return (float)System.Math.Acos(input);
        }

        //float version not in System.Math
        public static float Atan(this float input)
        {
            return (float)System.Math.Atan(input);
        }

        //float version not in System.Math
        public static float Atan2(float y, float x)
        {
            return (float)System.Math.Atan2(y, x);
        }
        
        public static float Pow(this float input, float exponent)
        {
        	return (float)System.Math.Pow(input, exponent);
        }

        public static float Sqrt(this float input)
        {
            return (float)System.Math.Sqrt(input);
        }

        public static float Ln(this float input)
        {
            return (float)System.Math.Log(input);
        }

        public static float Log(this float input, float newBase)
        {
            return (float)System.Math.Log(input, newBase);
        }

        public static int Round(this float input)
        {
            return input > 0 ? (int)(input + 0.5f) : (int)(input - 0.5f);
        }

        public static void Frac(this float input, out int wholePart, out float fractionalPart)
        {
            wholePart = (int)input;
            fractionalPart = input - wholePart;
        }

        public static void FracSawtooth(this float input, out int wholePart, out float fractionalPart)
        {
            wholePart = (int)input;
            fractionalPart = input - wholePart;

            if (input < 0)
            {
                wholePart--;
                fractionalPart++;
            }
        }

        public static void FracFloat(this float input, out float wholePart, out float fractionalPart)
        {
            wholePart = (int)input;
            fractionalPart = input - wholePart;
        }

        public static void FracFloatSawtooth(this float input, out float wholePart, out float fractionalPart)
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
        public static int Floor(this float input)
        {
            var integer = (int)input;
            if (integer > input) integer--;
            return integer;
        }

        //2x faster
        public static int Ceil(this float input)
        {
            var integer = (int)input;
            if (integer < input) integer++;
            return integer;
        }

        public static float RoundFloat(this float input)
        {
            return input > 0 ? (int)(input + 0.5f) : (int)(input - 0.5f);
        }

        //2x faster
        public static float FloorFloat(this float input)
        {
            var integer = (int)input;
            if (integer > input) integer--;
            return integer;
        }

        //2x faster
        public static float CeilFloat(this float input)
        {
            var integer = (int)input;
            if (integer < input) integer++;
            return integer;
        }

        public const float PI = (float)System.Math.PI;
        public const  float TwoPI = (float)Float64Extensions.TwoPI;
        public const float cyclesToRadians = (float)Float64Extensions.CyclesToRadians64;
        public const float radiansToCycles = (float)Float64Extensions.RadiansToCycles64;
        const float DegreeToCycles32 = (float)Float64Extensions.DegreeToCycles64;

        public static float CyclesToRadians(this float cycles)
        {
            return cyclesToRadians * cycles;
        }

        public static float RadiansToCycles(this float radian)
        {
            return radiansToCycles * radian;
        }

        public static float CyclesToDegree(this float cycles)
        {
            return cycles * 360;
        }

        public static float DegreeToCycles(this float degree)
        {
            return degree * DegreeToCycles32;
        }

        public static float Scale(this float input, float scalar)
        {
            return input * scalar;
        }

        public static float DivideScale(this float input, float scalar = 1f)
        {
            return input / scalar;
        }

        public static float Length(this float input)
        {
            return Math.Abs(input);
        }

        public static float LengthSquared(this float input)
        {
            return input * input;
        }

        public static float Normalize(this float input)
        {
            return Math.Sign(input);
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
        public static float CatmullRom(float value1, float value2, float value3, float value4, float amount)
        {
            float squared = amount * amount;
            float cubed = amount * squared;

            return 0.5f * ((((2.0f * value2) + ((-value1 + value3) * amount)) +
            (((((2.0f * value1) - (5.0f * value2)) + (4.0f * value3)) - value4) * squared)) +
            ((((-value1 + (3.0f * value2)) - (3.0f * value3)) + value4) * cubed));

        }

    }
}
