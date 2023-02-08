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

namespace VL.Lib.Mathematics
{
    public static class Vector4Nodes
    {

        /// <summary>
        /// Splits a vector into its components
        /// </summary>
        /// <param name="input"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="w"></param>
        public static void Vector(ref Vector4 input, out float x, out float y, out float z, out float w)
        {
            x = input.X;
            y = input.Y;
            z = input.Z;
            w = input.W;
        }

        /// <summary>
        /// Gets the specified component of the vector, throws an exception when out of bounds. Use GetSlice if you want to auto wrap the index
        /// </summary>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static float GetItem(ref Vector4 input, int index)
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
        public static void SetItem(ref Vector4 input, float value, int index, out Vector4 result)
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
        public static float GetSlice(ref Vector4 input, int index)
        {
            return input[index.ZMOD(4)];
        }

        /// <summary>
        /// Sets the specified component of the vector, wraps the index if out of bounds. Use SetItem if you want better performance
        /// </summary>
        /// <param name="input"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <param name="result"></param>
        public static void SetSlice(ref Vector4 input, float value, int index, out Vector4 result)
        {
            result = input;
            result[index.ZMOD(4)] = value;
        }

        public static Vector4 MOD(ref Vector4 input, ref Vector4 input2)
        {
            return new Vector4(input.X % input2.X,
                input.Y % input2.Y,
                input.Z % input2.Z,
                input.W % input2.W);
        }



        public static Vector4 Lerp(ref Vector4 input, ref Vector4 input2, float scalar)
        {
            return new Vector4(
                input.X + (input2.X - input.X) * scalar,
                input.Y + (input2.Y - input.Y) * scalar,
                input.Z + (input2.Z - input.Z) * scalar,
                input.W + (input2.W - input.W) * scalar);
            //input.X * (1 - scalar) + input2.X * scalar better?
        }

        public static void Scale(ref Vector4 input, out Vector4 output, float scalar = 1)
        {
            output.X = input.X * scalar;
            output.Y = input.Y * scalar;
            output.Z = input.Z * scalar;
            output.W = input.W * scalar;
        }

        public static void DivScale(ref Vector4 input, out Vector4 output, float scalar = 1)
        {
            scalar = 1 / scalar;
            output.X = input.X * scalar;
            output.Y = input.Y * scalar;
            output.Z = input.Z * scalar;
            output.W = input.W * scalar;
        }

        /// <summary>
        /// Multiplies a vector with another by performing component-wise multiplication.
        /// </summary>
        /// <param name="left">The first vector to multiply.</param>
        /// <param name="right">The second vector to multiply.</param>
        /// <param name="result">When the method completes, contains the multiplied vector.</param>
        public static void Multiply(ref Vector4 left, ref Vector4 right, out Vector4 result)
        {
            result = new Vector4(left.X * right.X, left.Y * right.Y, left.Z * right.Z, left.W * right.W);
        }

        public static Spread<float> ToValues(ref Vector4 input)
        {
            return Spread.Create(ImmutableArray.Create(input.X, input.Y, input.Z, input.W));
        }

        /// <summary>
        /// Creates a vector from the first values of a sequence, if the count of the sequence is lower than the dimension the remainig values get filled with 0
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static Vector4 FromValues(IEnumerable<float> values)
        {
            var vector = new Vector4();

            var iterator = values.GetEnumerator();

            if (iterator.MoveNext())
                vector.X = iterator.Current;

            if (iterator.MoveNext())
                vector.Y = iterator.Current;

            if (iterator.MoveNext())
                vector.Z = iterator.Current;

            if (iterator.MoveNext())
                vector.W = iterator.Current;

            return vector;
        }
    }
}
