using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace VL.Lib.Adaptive
{
    public static class AdaptiveNodes
    {
        //public static string toString<T>(T input)
        //{
        //    throw new NotImplementedException();
        //}

        public static bool IsNaN<T>(T input)
        {
            throw new NotImplementedException();
        }

        public static T IsNaNComponent<T>(T input)
        {
            throw new NotImplementedException();
        }

        public static T AvoidNaN<T>(T input, T @default)
        {
            throw new NotImplementedException();
        }

        public static bool equals<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        public static bool doesNotEqual<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        public static bool smallerThan<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        public static bool smallerThanOrEqual<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        public static bool biggerThan<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        public static bool biggerThanOrEqual<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        public static T equalsComponent<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        public static T doesNotEqualComponent<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        public static T smallerThanComponent<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        public static T smallerThanOrEqualComponent<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        public static T biggerThanComponent<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        public static T biggerThanOrEqualComponent<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        public static T plusIdentity<T>(T input)
        {
            throw new NotImplementedException();
        }

        public static T minusNegate<T>(T input)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Plus operator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static T plus<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Minus operator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static T minus<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Multiply operator; the inverse operation of division
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static T multiply<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Division operator; the inverse operation of multiplication
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static T divide<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Integer division, ie. the fractional part (remainder) is being discarded
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static T divideInteger<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Modulo operator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static T mod<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Modulo operator with the property, that the remainder of a division z / d is always positive. For example: zmod(-2, 30) = 28.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static T zmod<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Bitwise negation for integer types
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static T onesComplement<T>(T input)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Bitwise OR for integer types
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static T orBitwise<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Bitwise AND for integer types
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static T andBitwise<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Bitwise XOR for integer types
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static T xorBitwise<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The zero object for a specific type, the identity element for the addition operation
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static void Zero<T>(out T zero)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The one object for a specific type, the identity element for the multiplication operation
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static void One<T>(out T one)
        {
            throw new NotImplementedException();
        }

        public static int Sign<T>(T input)
        {
            throw new NotImplementedException();
        }

        public static float Length<T>(T input)
        {
            throw new NotImplementedException();
        }

        public static float LengthSquared<T>(T input)
        {
            throw new NotImplementedException();
        }

        public static float Dot<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        public static T Scale<T>(T input, float scalar)
        {
            throw new NotImplementedException();
        }

        public static T DivideScale<T>(T input, float scalar)
        {
            throw new NotImplementedException();
        }

        public static T Lerp<T>(T input, T input2, float scalar)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Puts the value into a grid with given step size
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="stepSize"></param>
        /// <returns></returns>
        public static T Quantize<T>(T input, T stepSize)
        {
            throw new NotImplementedException();
        }

        //public static T Vector<T, E>(E x, E y)
        //{
        //    throw new NotImplementedException();
        //}

        //public static void Vector<T, E>(T input, out E x, out E y)
        //{
        //    throw new NotImplementedException();
        //}

        //public static T Vector<T, E>(E x, E y, E z)
        //{
        //    throw new NotImplementedException();
        //}

        //public static void Vector<T, E>(T input, out E x, out E y, out E z)
        //{
        //    throw new NotImplementedException();
        //}

        public static T Sin<T>(T input)
        {
            throw new NotImplementedException();
        }

        public static T Cos<T>(T input)
        {
            throw new NotImplementedException();
        }

        public static T Tan<T>(T input)
        {
            throw new NotImplementedException();
        }

        public static T Asin<T>(T input)
        {
            throw new NotImplementedException();
        }

        public static T Acos<T>(T input)
        {
            throw new NotImplementedException();
        }

        public static T Atan<T>(T input)
        {
            throw new NotImplementedException();
        }

        public static T Atan2<T>(T y, T x)
        {
            throw new NotImplementedException();
        }

        public static T Normalize<T>(T input)
        {
            throw new NotImplementedException();
        }

        public static int Round<F>(this F input)
        {
            throw new NotImplementedException();
        }

        public static void Frac<F>(this F input, out int wholePart, out F fractionalPart)
        {
            throw new NotImplementedException();
        }


        public static void FracSawtooth<F>(this F input, out int wholePart, out F fractionalPart)
        {
            throw new NotImplementedException();
        }

        public static int Floor<F>(this F input)
        {
            throw new NotImplementedException();
        }

        public static int Ceil<F>(this F input)
        {
            throw new NotImplementedException();
        }

        public static F RoundFloat<F>(this F input)
        {
            throw new NotImplementedException();
        }

        public static void FracFloat<F>(this F input, out F wholePart, out F fractionalPart)
        {
            throw new NotImplementedException();
        }

        public static void FracFloatSawtooth<F>(this F input, out F wholePart, out F fractionalPart)
        {
            throw new NotImplementedException();
        }

        public static F FloorFloat<F>(this F input)
        {
            throw new NotImplementedException();
        }

        public static F CeilFloat<F>(this F input)
        {
            throw new NotImplementedException();
        }

        public static F Abs<F>(this F input)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Outputs the smaller value of the two inputs
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static T Min<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Outputs the greater value of the two inputs
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static T Max<T>(T input, T input2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clamps the input into 0..1 range
        /// </summary>
        /// <typeparam name="F"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static F Saturate<F>(this F input)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clamps the input into Minimum..Maximum range
        /// </summary>
        /// <typeparam name="F"></typeparam>
        /// <param name="input"></param>
        /// <param name="Minimum"></param>
        /// <param name="Maximum"></param>
        /// <returns></returns>
        public static F Clamp<F>(this F input, F Minimum, F Maximum)
        {
            throw new NotImplementedException();
        }

        public static T Pow<T>(this T input, T exponent)
        {
            throw new NotImplementedException();
        }

        public static T Sqrt<T>(this T input)
        {
            throw new NotImplementedException();
        }

        public static T Ln<T>(this T input)
        {
            throw new NotImplementedException();
        }

        public static T Log<T>(this T input, T newBase)
        {
            throw new NotImplementedException();
        }

        public static T Random<T>(T from, T to)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Samples a smooth noise function at the input position.
        /// </summary>
        /// <remarks>
        /// Output range is roughly [-1..1]
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static float Simplex<T>(T input)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Samples a smooth noise function at the input position.
        /// The additional Scalar input animates the noise function.
        /// </summary>
        /// <remarks>
        /// Output range is roughly [-1..1]
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static float Simplex<T>(T input, float scalar)
        {
            throw new NotImplementedException();
        }

        public static T PI<T>()
        {
            throw new NotImplementedException();
        }

        public static T TwoPI<T>()
        {
            throw new NotImplementedException();
        }

        public static T CyclesToRadians<T>(T input)
        {
            throw new NotImplementedException();
        }

        public static T RadiansToCycles<T>(T input)
        {
            throw new NotImplementedException();
        }

        public static T CyclesToDegree<T>(T input)
        {
            throw new NotImplementedException();
        }

        public static T DegreeToCycles<T>(T input)
        {
            throw new NotImplementedException();
        }

        public static T Confine<T>(T input, T velocity, T min, T max, out T velocityOut, out bool gotReflected)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs a Catmull-Rom interpolation using the specified positions
        /// </summary>
        public static T CatmullRom<T>(T value1, T value2, T value3, T value4, float amount)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs a Hermite spline interpolation
        /// </summary>
        public static T Hermite<T>(T value1, T tangent1, T value2, T tangent2, float amount)
        {
            throw new NotImplementedException();
        }

        public static string ToString<T>(this T input, string format)
        {
            throw new NotImplementedException();
        }

        public static void Clone<T>(T input, out T original, out T clone)
        {
            throw new NotImplementedException();
        }

        public static ReadOnlyMemory<TElement> AsReadOnlyMemory<TInput, TElement>(TInput input) 
            where TInput : IEnumerable<TElement>
        {
            throw new NotImplementedException();
        }

        public static Memory<TElement> AsMemory<TInput, TElement>(TInput input) 
            where TInput : IEnumerable<TElement>
        {
            throw new NotImplementedException();
        }
    }
}
