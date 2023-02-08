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
    public static class Vector3Nodes
    {

        /// <summary>
        /// Splits a vector into its components
        /// </summary>
        /// <param name="input"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public static void Vector(ref Vector3 input, out float x, out float y, out float z)
        {
            x = input.X;
            y = input.Y;
            z = input.Z;
        }

        /// <summary>
        /// Converts to a Vector4 with a specified w
        /// </summary>
        /// <param name="input"></param>
        /// <param name="w"></param>
        /// <returns></returns>
        public static Vector4 ToVector4(ref Vector3 input, float w = 1)
        {
            return new Vector4(input.X, input.Y, input.Z, w);
        }

        /// <summary>
        /// Gets the specified component of the vector, throws an exception when out of bounds. Use GetSlice if you want to auto wrap the index
        /// </summary>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static float GetItem(ref Vector3 input, int index)
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
        public static void SetItem(ref Vector3 input, float value, int index, out Vector3 result)
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
        public static float GetSlice(ref Vector3 input, int index)
        {
            return input[index.ZMOD(3)];
        }

        /// <summary>
        /// Sets the specified component of the vector, wraps the index if out of bounds. Use SetItem if you want better performance
        /// </summary>
        /// <param name="input"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <param name="result"></param>
        public static void SetSlice(ref Vector3 input, float value, int index, out Vector3 result)
        {
            result = input;
            result[index.ZMOD(3)] = value;
        }

        public static Vector3 MOD(ref Vector3 input, ref Vector3 input2)
        {
            return new Vector3(input.X % input2.X,
                input.Y % input2.Y,
                input.Z % input2.Z);
        }

        public static Vector3 Lerp(ref Vector3 input, ref Vector3 input2, float scalar)
        {
            return new Vector3(
                input.X + (input2.X - input.X) * scalar,
                input.Y + (input2.Y - input.Y) * scalar,
                input.Z + (input2.Z - input.Z) * scalar);
            //input.X * (1 - scalar) + input2.X * scalar better?
        }

        public static void Scale(ref Vector3 input, out Vector3 output, float scalar = 1)
        {
            output.X = input.X * scalar;
            output.Y = input.Y * scalar;
            output.Z = input.Z * scalar;
        }

        public static void DivScale(ref Vector3 input, out Vector3 output, float scalar = 1)
        {
            scalar = 1 / scalar;
            output.X = input.X * scalar;
            output.Y = input.Y * scalar;
            output.Z = input.Z * scalar;
        }

        /// <summary>
        /// Multiply a vector with another by performing component-wise multiplication.
        /// </summary>
        /// <param name="left">The first vector to multiply.</param>
        /// <param name="right">The second vector to multiply.</param>
        /// <param name="result">When the method completes, contains the multiplied vector.</param>
        public static void Multiply(ref Vector3 left, ref Vector3 right, out Vector3 result)
        {
            result = new Vector3(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
        }

        const double CRadiansToCycles = 1 / (2 * Math.PI);

        /// <summary>
        /// Calculates yaw and pitch for the direction of a Vector3 in cycles, the lenght is output as well since its calculated in the process anyway
        /// </summary>
        /// <param name="input"></param>
        /// <param name="polar"></param>
        /// <param name="azimuthal"></param>
        /// <param name="length"></param>
        public static void Angle(ref Vector3 input, out float polar, out float azimuthal, out float length)
        {
            var lengthSqrt = input.LengthSquared();

            if (lengthSqrt > 0)
            {
                length = (float)Math.Sqrt(lengthSqrt);
                polar = (float)(Math.Acos(input.Y / length) * CRadiansToCycles);
                azimuthal = (float)(Math.Atan2(input.X, -input.Z) * CRadiansToCycles);                         
            }
            else
            {
                length = 0;
                polar = 0;
                azimuthal = 0;                
            }
        }

        public static Spread<float> ToValues(ref Vector3 input)
        {
            return Spread.Create(ImmutableArray.Create(input.X, input.Y, input.Z));
        }

        /// <summary>
        /// Creates a vector from the first values of a sequence, if the count of the sequence is lower than the dimension the remainig values get filled with 0
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static Vector3 FromValues(IEnumerable<float> values)
        {
            var vector = new Vector3();

            var iterator = values.GetEnumerator();

            if (iterator.MoveNext())
                vector.X = iterator.Current;

            if (iterator.MoveNext())
                vector.Y = iterator.Current;

            if (iterator.MoveNext())
                vector.Z = iterator.Current;

            return vector;
        }
    }
}
