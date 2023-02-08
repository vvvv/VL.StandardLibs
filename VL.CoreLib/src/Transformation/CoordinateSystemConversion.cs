using MathNet.Numerics.LinearAlgebra;
using Stride.Core.Mathematics;

namespace VL.CoordinateSystem
{
    public enum CoordinateSystemType { LeftHanded, RightHanded };

    public static class CoordinateSystemHelper
    {
        public static Matrix ConversionMatrix(
            Matrix source,
            Matrix target)
        {
            var conversionMatrix = MatrixToDNMatrix(source).LU().Solve(MatrixToDNMatrix(target));
            return DNMatrixToMatrix(conversionMatrix);
        }

        public static Matrix<float> MatrixToDNMatrix(Matrix m)
        {
            return CreateMatrix.DenseOfArray(
                new float[,] {
                    { m[0,0], m[0,1], m[0,2], m[0,3] },
                    { m[1,0], m[1,1], m[1,2], m[1,3] },
                    { m[2,0], m[2,1], m[2,2], m[2,3] },
                    { m[3,0], m[3,1], m[3,2], m[3,3] }
                });
        }

        private static Matrix DNMatrixToMatrix(Matrix<float> m)
        {
            Matrix result = new Matrix();
            for (int row = 0; row < m.RowCount; row++)
            {
                for (int col = 0; col < m.ColumnCount; col++)
                {
                    result[row, col] = m[row, col];
                }
            }
            return result;
        }
    }
}
