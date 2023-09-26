using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using VL.Core;
using VL.Lib.Primitive;

namespace VL.Lib.Mathematics
{
    public static class QuaternionNodes
    {
        public static float Angle(ref Quaternion input)
        {
            return input.Angle * Float32Extensions.radiansToCycles;
        }

        public static void Quaternion(ref Vector4 input, out Quaternion output)
        {
            output = new Quaternion(input.X, input.Y, input.Z, input.W);
        }

        /// <summary>
        /// Splits a quaternion into its components
        /// </summary>
        /// <param name="input"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="w"></param>
        public static void Quaternion(ref Quaternion input, out float x, out float y, out float z, out float w)
        {
            x = input.X;
            y = input.Y;
            z = input.Z;
            w = input.W;
        }

        public static void ToVector4(ref Quaternion input, out Vector4 output)
        {
            output = new Vector4(input.X, input.Y, input.Z, input.W);
        }

        public static void Scale(ref Quaternion input, out Quaternion output, float scalar = 1)
        {
            output.X = input.X * scalar;
            output.Y = input.Y * scalar;
            output.Z = input.Z * scalar;
            output.W = input.W * scalar;
        }

        public static void DivScale(ref Quaternion input, out Quaternion output, float scalar = 1)
        {
            scalar = 1 / scalar;
            output.X = input.X * scalar;
            output.Y = input.Y * scalar;
            output.Z = input.Z * scalar;
            output.W = input.W * scalar;
        }

        public static void RotationAxis(ref Vector3 axis, float angle, out Quaternion result)
        {
            Stride.Core.Mathematics.Quaternion.RotationAxis(ref axis,
                angle * Float32Extensions.cyclesToRadians, 
                out result);
        }

        public static void Rotation(float pitch, float yaw, float roll, out Quaternion result)
        {
            Stride.Core.Mathematics.Quaternion.RotationYawPitchRoll(yaw * Float32Extensions.cyclesToRadians,
                pitch * Float32Extensions.cyclesToRadians,
                roll * Float32Extensions.cyclesToRadians, out result);
        }

        public static void RotationVector(Vector3 pitchYawRoll, out Quaternion result)
        {
            Stride.Core.Mathematics.Quaternion.RotationYawPitchRoll(pitchYawRoll.Y * Float32Extensions.cyclesToRadians,
                pitchYawRoll.X * Float32Extensions.cyclesToRadians,
                pitchYawRoll.Z * Float32Extensions.cyclesToRadians, out result);
        }

        /// <summary>
        /// Converts a quaternion into euler angles, assuming that the euler angle multiplication to create the quaternion was yaw*pitch*roll.
        /// Output angles are in cycles.
        /// </summary>
        /// <param name="q">A quaternion, can be non normalized</param>
        /// <param name="pitch"></param>
        /// <param name="yaw"></param>
        /// <param name="roll"></param>
        public static void QuaternionToEulerYawPitchRoll(Quaternion q, out float pitch, out float yaw, out float roll)
        {
            double sqw = q.W * q.W;
            double sqx = q.X * q.X;
            double sqy = q.Y * q.Y;
            double sqz = q.Z * q.Z;
            double unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
            double test = q.X * q.W - q.Y * q.Z;

            if (test > 0.4999 * unit)
            { // singularity at north pole
                pitch = 0.25f;
                yaw = (float)(2 * Math.Atan2(q.Y, q.W) * Float64Extensions.RadiansToCycles64);
                roll = 0;
                return;
            }

            if (test < -0.4999 * unit)
            { // singularity at south pole
                pitch = -0.25f;
                yaw = (float)(2 * Math.Atan2(q.Y, q.W) * Float64Extensions.RadiansToCycles64);
                roll = 0;
                return;
            }

            pitch = (float)(Math.Asin(2 * (q.W * q.X - q.Y * q.Z) / unit) * Float64Extensions.RadiansToCycles64);
            yaw = (float)(Math.Atan2(2 * (q.W * q.Y + q.X * q.Z), 1 - 2 * (sqy + sqx)) * Float64Extensions.RadiansToCycles64);
            roll = (float)(Math.Atan2(2 * (q.W * q.Z + q.Y * q.X), 1 - 2 * (sqx + sqz)) * Float64Extensions.RadiansToCycles64);
        }

        /// <summary>
        /// Creates a left-handed, look-at quaternion.
        /// </summary>
        /// <param name="eye">The position of the viewer's eye.</param>
        /// <param name="target">The camera look-at target.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <param name="result">When the method completes, contains the created look-at quaternion.</param>
        public static void LookAtLH(ref Vector3 eye, ref Vector3 target, ref Vector3 up, out Quaternion result)
        {
            Matrix3x3 matrix;
            Matrix3x3.LookAtLH(ref eye, ref target, ref up, out matrix);
            RotationMatrix(ref matrix, out result);
        }

        /// <summary>
        /// Creates a left-handed, look-at quaternion.
        /// </summary>
        /// <param name="forward">The camera's forward direction.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <param name="result">When the method completes, contains the created look-at quaternion.</param>
        public static void RotationLookAtLH(ref Vector3 forward, ref Vector3 up, out Quaternion result)
        {
            Vector3 eye = Vector3.Zero;
            QuaternionNodes.LookAtLH(ref eye, ref forward, ref up, out result);
        }

        /// <summary>
        /// Creates a right-handed, look-at quaternion.
        /// </summary>
        /// <param name="eye">The position of the viewer's eye.</param>
        /// <param name="target">The camera look-at target.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <param name="result">When the method completes, contains the created look-at quaternion.</param>
        public static void LookAtRH(ref Vector3 eye, ref Vector3 target, ref Vector3 up, out Quaternion result)
        {
            Matrix3x3 matrix;
            Matrix3x3.LookAtRH(ref eye, ref target, ref up, out matrix);
            RotationMatrix(ref matrix, out result);
        }

        /// <summary>
        /// Creates a right-handed, look-at quaternion.
        /// </summary>
        /// <param name="forward">The camera's forward direction.</param>
        /// <param name="up">The camera's up vector.</param>
        /// <param name="result">When the method completes, contains the created look-at quaternion.</param>
        public static void RotationLookAtRH(ref Vector3 forward, ref Vector3 up, out Quaternion result)
        {
            Vector3 eye = Vector3.Zero;
            QuaternionNodes.LookAtRH(ref eye, ref forward, ref up, out result);
        }

        /// <summary>
        /// Creates a quaternion given a rotation matrix.
        /// </summary>
        /// <param name="matrix">The rotation matrix.</param>
        /// <param name="result">When the method completes, contains the newly created quaternion.</param>
        public static void RotationMatrix(ref Matrix3x3 matrix, out Quaternion result)
        {
            float sqrt;
            float half;
            float scale = matrix.M11 + matrix.M22 + matrix.M33;

            if (scale > 0.0f)
            {
                sqrt = (float)Math.Sqrt(scale + 1.0f);
                result.W = sqrt * 0.5f;
                sqrt = 0.5f / sqrt;

                result.X = (matrix.M23 - matrix.M32) * sqrt;
                result.Y = (matrix.M31 - matrix.M13) * sqrt;
                result.Z = (matrix.M12 - matrix.M21) * sqrt;
            }
            else if ((matrix.M11 >= matrix.M22) && (matrix.M11 >= matrix.M33))
            {
                sqrt = (float)Math.Sqrt(1.0f + matrix.M11 - matrix.M22 - matrix.M33);
                half = 0.5f / sqrt;

                result.X = 0.5f * sqrt;
                result.Y = (matrix.M12 + matrix.M21) * half;
                result.Z = (matrix.M13 + matrix.M31) * half;
                result.W = (matrix.M23 - matrix.M32) * half;
            }
            else if (matrix.M22 > matrix.M33)
            {
                sqrt = (float)Math.Sqrt(1.0f + matrix.M22 - matrix.M11 - matrix.M33);
                half = 0.5f / sqrt;

                result.X = (matrix.M21 + matrix.M12) * half;
                result.Y = 0.5f * sqrt;
                result.Z = (matrix.M32 + matrix.M23) * half;
                result.W = (matrix.M31 - matrix.M13) * half;
            }
            else
            {
                sqrt = (float)Math.Sqrt(1.0f + matrix.M33 - matrix.M11 - matrix.M22);
                half = 0.5f / sqrt;

                result.X = (matrix.M31 + matrix.M13) * half;
                result.Y = (matrix.M32 + matrix.M23) * half;
                result.Z = 0.5f * sqrt;
                result.W = (matrix.M12 - matrix.M21) * half;
            }
        }
    }
}
