using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using VL.Core;
using VL.Lib.Primitive;
using VL.Lib.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace VL.Lib.Mathematics
{
    public static class Vector2Nodes
    {

        /// <summary>
        /// Splits a vector into its components
        /// </summary>
        /// <param name="input"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void Vector(ref Vector2 input, out float x, out float y)
        {
            x = input.X;
            y = input.Y;
        }

        /// <summary>
        /// Determines whether the specified <see cref="Vector2"/> is equal to this instance.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="other">The <see cref="Vector2"/> to compare with this instance.</param>
        /// <returns>
        /// 	<c>true</c> if the specified <see cref="Vector2"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(ref Vector2 input, ref Vector2 other)
        {
            return MathUtil.NearEqual(input.X, other.X) && MathUtil.NearEqual(input.Y, other.Y);
        }

        /// <summary>
        /// Converts to a Vector3 with a specified z
        /// </summary>
        /// <param name="input"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static Vector3 ToVector3(ref Vector2 input, float z)
        {
            return new Vector3(input.X, input.Y, z);
        }

        /// <summary>
        /// Converts to a Vector4 with a specified z and w
        /// </summary>
        /// <param name="input"></param>
        /// <param name="z"></param>
        /// <param name="w"></param>
        /// <returns></returns>
        public static Vector4 ToVector4(ref Vector2 input, float z, float w = 1)
        {
            return new Vector4(input.X, input.Y, z, w);
        }

        /// <summary>
        /// Gets the specified component of the vector, throws an exception when out of bounds. Use GetSlice if you want to auto wrap the index
        /// </summary>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static float GetItem(ref Vector2 input, int index)
        {
            return input[index];
        }

        /// <summary>
        /// Sets the specified component of the vector, throws an exception when out of bounds. Use SetSlice if you want to auto wrap the index
        /// </summary>
        /// <param name="input"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <param name="result"></param>
        public static void SetItem(ref Vector2 input, float value, int index, out Vector2 result)
        {
            result = input;
            result[index] = value;
        }

        /// <summary>
        /// Gets the specified component of the vector, wraps the index if out of bounds. Use GetItem if you want better performance
        /// </summary>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static float GetSlice(ref Vector2 input, int index)
        {
            return input[index.ZMOD(2)];
        }

        /// <summary>
        /// Sets the specified component of the vector, wraps the index if out of bounds. Use SetItem if you want better performance
        /// </summary>
        /// <param name="input"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <param name="result"></param>
        public static void SetSlice(ref Vector2 input, float value, int index, out Vector2 result)
        {
            result = input;
            result[index.ZMOD(2)] = value;
        }

        public static Vector2 MOD(ref Vector2 input, ref Vector2 input2)
        {
            return new Vector2(input.X % input2.X, input.Y % input2.Y);
        }

        public static Vector2 Lerp(ref Vector2 input, ref Vector2 input2, float scalar)
        {
            return new Vector2(
                input.X + (input2.X - input.X) * scalar, 
                input.Y + (input2.Y - input.Y) * scalar);
            //input.X * (1 - scalar) + input2.X * scalar bettter?
        }

        public static void Scale(ref Vector2 input, out Vector2 output, float scalar = 1)
        {
            output.X = input.X * scalar;
            output.Y = input.Y * scalar;
        }

        public static void DivScale(ref Vector2 input, out Vector2 output, float scalar = 1)
        {
            scalar = 1 / scalar;
            output.X = input.X * scalar;
            output.Y = input.Y * scalar;
        }

        /// <summary>
        /// Multiplies a vector with another by performing component-wise multiplication.
        /// </summary>
        /// <param name="left">The first vector to multiply.</param>
        /// <param name="right">The second vector to multiply.</param>
        /// <param name="result">When the method completes, contains the multiplied vector.</param>
        public static void Multiply(ref Vector2 left, ref Vector2 right, out Vector2 result)
        {
            result = new Vector2(left.X * right.X, left.Y * right.Y);
        }

        /// <summary>
        /// Two dimensional cross product, also called perp dot product 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static float Cross(ref Vector2 input, ref Vector2 input2)
        {
            return input.X * input2.Y - input.Y * input2.X;
        }

        const double CRadiansToCycles = 1 / (2 * Math.PI);

        /// <summary>
        /// Calculates the angle between the direction of a 2d vector and the X-Axis in cycles
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static float Angle(ref Vector2 input)
        {
            return (float)(Math.Atan2(input.Y, input.X) * CRadiansToCycles);
        }

        public static Spread<float> ToValues(ref Vector2 input)
        {
            return Spread.Create(ImmutableArray.Create(input.X, input.Y));
        }

        /// <summary>
        /// Creates a vector from the first values of a sequence, if the count of the sequence is lower than the dimension the remainig values get filled with 0
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static Vector2 FromValues(IEnumerable<float> values)
        {
            var vector = new Vector2();

            var iterator = values.GetEnumerator();

            if (iterator.MoveNext())
                vector.X = iterator.Current;

            if (iterator.MoveNext())
                vector.Y = iterator.Current;

            return vector;
        }
    }
}
