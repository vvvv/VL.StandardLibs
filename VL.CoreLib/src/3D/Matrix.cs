using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using VL.Core;
using VL.Lib.Collections;
using VL.Lib.Primitive;
using System.Collections.Immutable;
using static System.FormattableString;

namespace VL.Lib.Mathematics
{
    public static class MatrixNodes
    {
        public static void MatrixJoin(Spread<float> elements, out Matrix result)
        {
            result = new Matrix(elements[0],  elements[1],  elements[2],  elements[3],
                                elements[4],  elements[5],  elements[6],  elements[7],
                                elements[8],  elements[9],  elements[10], elements[11],
                                elements[12], elements[13], elements[14], elements[15]);
        }

        /// <summary>
        /// Returns all individual values of the matrix.
        /// </summary>
        public static void MatrixSplit(ref Matrix input, 
            out float M11, out float M12, out float M13, out float M14,
            out float M21, out float M22, out float M23, out float M24,
            out float M31, out float M32, out float M33, out float M34,
            out float M41, out float M42, out float M43, out float M44)
        {
            M11 = input.M11; M12 = input.M12; M13 = input.M13; M14 = input.M14;
            M21 = input.M21; M22 = input.M22; M23 = input.M23; M24 = input.M24;
            M31 = input.M31; M32 = input.M32; M33 = input.M33; M34 = input.M34;
            M41 = input.M41; M42 = input.M42; M43 = input.M43; M44 = input.M44;
        }

        /// <summary>
        /// Gets the matrix values as a spread of floats
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Spread<float> GetValues(ref Matrix input)
        {
            var values = ImmutableArray.Create(
                input.M11, input.M12, input.M13, input.M14,
                input.M21, input.M22, input.M23, input.M24,
                input.M31, input.M32, input.M33, input.M34,
                input.M41, input.M42, input.M43, input.M44);
            return Spread.Create(values);
        }

        /// <summary>
        /// Transforms one matrix by another
        /// </summary>
        /// <param name="transformation">The first matrix to multiply.</param>
        /// <param name="input">The second matrix to multiply.</param>
        /// <param name="result">The product of the two matrices.</param>
        public static void Transform(ref Matrix input, ref Matrix transformation, out Matrix result)
        {
            result = new Matrix(
                (transformation.M11 * input.M11) + (transformation.M12 * input.M21) + (transformation.M13 * input.M31) + (transformation.M14 * input.M41),
                (transformation.M11 * input.M12) + (transformation.M12 * input.M22) + (transformation.M13 * input.M32) + (transformation.M14 * input.M42),
                (transformation.M11 * input.M13) + (transformation.M12 * input.M23) + (transformation.M13 * input.M33) + (transformation.M14 * input.M43),
                (transformation.M11 * input.M14) + (transformation.M12 * input.M24) + (transformation.M13 * input.M34) + (transformation.M14 * input.M44),
                (transformation.M21 * input.M11) + (transformation.M22 * input.M21) + (transformation.M23 * input.M31) + (transformation.M24 * input.M41),
                (transformation.M21 * input.M12) + (transformation.M22 * input.M22) + (transformation.M23 * input.M32) + (transformation.M24 * input.M42),
                (transformation.M21 * input.M13) + (transformation.M22 * input.M23) + (transformation.M23 * input.M33) + (transformation.M24 * input.M43),
                (transformation.M21 * input.M14) + (transformation.M22 * input.M24) + (transformation.M23 * input.M34) + (transformation.M24 * input.M44),
                (transformation.M31 * input.M11) + (transformation.M32 * input.M21) + (transformation.M33 * input.M31) + (transformation.M34 * input.M41),
                (transformation.M31 * input.M12) + (transformation.M32 * input.M22) + (transformation.M33 * input.M32) + (transformation.M34 * input.M42),
                (transformation.M31 * input.M13) + (transformation.M32 * input.M23) + (transformation.M33 * input.M33) + (transformation.M34 * input.M43),
                (transformation.M31 * input.M14) + (transformation.M32 * input.M24) + (transformation.M33 * input.M34) + (transformation.M34 * input.M44),
                (transformation.M41 * input.M11) + (transformation.M42 * input.M21) + (transformation.M43 * input.M31) + (transformation.M44 * input.M41),
                (transformation.M41 * input.M12) + (transformation.M42 * input.M22) + (transformation.M43 * input.M32) + (transformation.M44 * input.M42),
                (transformation.M41 * input.M13) + (transformation.M42 * input.M23) + (transformation.M43 * input.M33) + (transformation.M44 * input.M43),
                (transformation.M41 * input.M14) + (transformation.M42 * input.M24) + (transformation.M43 * input.M34) + (transformation.M44 * input.M44)
            );
        }

        public static void GetRows(ref Matrix input, out Vector4 row1, out Vector4 row2, out Vector4 row3, out Vector4 row4)
        {
            row1 = input.Row1;
            row2 = input.Row2;
            row3 = input.Row3;
            row4 = input.Row4;
        }

        public static void GetColumns(ref Matrix input, out Vector4 column1, out Vector4 column2, out Vector4 column3, out Vector4 column4)
        {
            column1 = input.Column1;
            column2 = input.Column2;
            column3 = input.Column3;
            column4 = input.Column4;
        }

        public static void FromRows(ref Vector4 row1, ref Vector4 row2, ref Vector4 row3, ref Vector4 row4, out Matrix output)
        {
            output = new Matrix();
            output.Row1 = row1;
            output.Row2 = row2;
            output.Row3 = row3;
            output.Row4 = row4;
        }

        public static void FromColumns(ref Vector4 column1, ref Vector4 column2, ref Vector4 column3, ref Vector4 column4, out Matrix output)
        {
            output = new Matrix();
            output.Column1 = column1;
            output.Column2 = column2;
            output.Column3 = column3;
            output.Column4 = column4;
        }

        public static void Rotation(float pitch, float yaw, float roll, out Matrix result)
        {
            Matrix.RotationYawPitchRoll(yaw * Float32Extensions.cyclesToRadians,
                pitch * Float32Extensions.cyclesToRadians,
                roll * Float32Extensions.cyclesToRadians, out result);
        }

        public static void RotationVector(Vector3 pitchYawRoll, out Matrix result)
        {
            Matrix.RotationYawPitchRoll(pitchYawRoll.Y * Float32Extensions.cyclesToRadians,
                pitchYawRoll.X * Float32Extensions.cyclesToRadians,
                pitchYawRoll.Z * Float32Extensions.cyclesToRadians, out result);
        }

        public static void PerspectiveFOV(float FOV, float aspect, float zNear, float zFar, out Matrix result)
        {
            Matrix.PerspectiveFovLH(FOV * Float32Extensions.cyclesToRadians, aspect, zNear, zFar, out result);
        }

        public static void PerspectiveFOVRH(float FOV, float aspect, float zNear, float zFar, out Matrix result)
        {
            Matrix.PerspectiveFovRH(FOV * Float32Extensions.cyclesToRadians, aspect, zNear, zFar, out result);
        }

        public static void PerspectiveOffCenterDistanceLH(float left, float right, float bottom, float top, float znear, float zfar, float zdist, out Matrix result)
        {
            float zRange = zfar / (zfar - znear);

            result = new Matrix();
            result.M11 = 2.0f * zdist / (right - left);
            result.M22 = 2.0f * zdist / (top - bottom);
            result.M31 = (left + right) / (left - right);
            result.M32 = (top + bottom) / (bottom - top);
            result.M33 = zRange;
            result.M34 = 1.0f;
            result.M43 = -znear * zRange;
        }

        public static void PerspectiveOffCenterDistanceRH(float left, float right, float bottom, float top, float znear, float zfar, float zdist, out Matrix result)
        {
            float zRange = zfar / (znear - zfar);
            float rml = right - left;
            float tmb = top - bottom;

            result = new Matrix();
            result.M11 = 2.0f * zdist / rml;
            result.M22 = 2.0f * zdist / tmb;
            result.M31 = (left + right) / rml;
            result.M32 = (top + bottom) / tmb;
            result.M33 = zRange;
            result.M34 = -1.0f;
            result.M43 = znear * zRange;
        }

        /// <summary>
        /// Very fast and direct solution of a 4-point homography, assumes that the original points are (0, 1), (1, 1), (1, 0) and (0, 0)
        /// </summary>
        /// <param name="P"></param>
        /// <param name="zScale"></param>
        /// <returns></returns>
        public static Matrix SolvePerspectiveUnitSquare(IReadOnlyList<Vector2> P, float zScale = 0.5f)
        {
            float T = 0;

            // Compute the transform coefficients
            T = (P[1].X - P[2].X) * (P[1].Y - P[0].Y) - (P[1].X - P[0].X) * (P[1].Y - P[2].Y);

            float G = ((P[1].X - P[3].X) * (P[1].Y - P[0].Y) - (P[1].X - P[0].X) * (P[1].Y - P[3].Y)) / T;
            float H = ((P[1].X - P[2].X) * (P[1].Y - P[3].Y) - (P[1].X - P[3].X) * (P[1].Y - P[2].Y)) / T;

            float A = G * (P[2].X - P[3].X);
            float D = G * (P[2].Y - P[3].Y);
            float B = H * (P[0].X - P[3].X);
            float E = H * (P[0].Y - P[3].Y);

            G -= 1;
            H -= 1;

            var projection = new Matrix 
            {
                M11 = A, M12 = D, M13 = 0, M14 = G,
                M21 = B, M22 = E, M23 = 0, M24 = H,
                M31 = 0, M32 = 0, M33 = zScale, M34 = 0,
                M41 = 0, M42 = 0, M43 = 0, M44 = 1
            };

            return projection * Matrix.Translation(P[3].X, P[3].Y, 0);
        }
    }
}
