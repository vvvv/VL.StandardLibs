using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.Core
{
    /// <summary>
    /// vvvv like modi for the Map function
    /// </summary>
    public enum MapMode
    {
        /// <summary>
        /// Maps the value continously
        /// </summary>
        Float,
        /// <summary>
        /// Maps the value, but clamps it at the min/max borders of the output range
        /// </summary>
        Clamp,
        /// <summary>
        /// Maps the value, but repeats it into the min/max range, like a modulo function
        /// </summary>
        Wrap,
        /// <summary>
        /// Maps the value, but mirrors it into the min/max range, always against either start or end, whatever is closer
        /// </summary>
        Mirror
    };

    /// <summary>
    /// The vvvv c# math routines library
    /// </summary>
    public sealed class VLMath
    {
        #region constants

        /// <summary>
        /// Pi, as you know it
        /// </summary>
        public const double Pi = 3.1415926535897932384626433832795;

        /// <summary>
        /// Pi * 2
        /// </summary>
        public const double TwoPi = 6.283185307179586476925286766559;

        /// <summary>
        /// 1 / Pi, multiply by this if you have to divide by Pi
        /// </summary>
        public const double PiRez = 0.31830988618379067153776752674503;

        /// <summary>
        /// 2 / Pi, multiply by this if you have to divide by 2*Pi
        /// </summary>
        public const double TwoPiRez = 0.15915494309189533576888376337251;

        /// <summary>
        /// Conversion factor from cycles to radians, (2 * Pi)
        /// </summary>
        public const double CycToRad = 6.28318530717958647693;
        /// <summary>
        /// Conversion factor from radians to cycles, 1/(2 * Pi)
        /// </summary>
        public const double RadToCyc = 0.159154943091895335769;
        /// <summary>
        /// Conversion factor from degree to radians, (2 * Pi)/360
        /// </summary>
        public const double DegToRad = 0.0174532925199432957692;
        /// <summary>
        /// Conversion factor from radians to degree, 360/(2 * Pi)
        /// </summary>
        public const double RadToDeg = 57.2957795130823208768;
        /// <summary>
        /// Conversion factor from degree to radians, 1/360
        /// </summary>
        public const double DegToCyc = 0.00277777777777777777778;
        /// <summary>
        /// Conversion factor from radians to degree, 360
        /// </summary>
        public const double CycToDeg = 360.0;

        /// <summary>
        /// Identity matrix 
        /// 1000 
        /// 0100
        /// 0010
        /// 0001
        /// </summary>
        public static readonly Matrix IdentityMatrix = new Matrix(1, 0, 0, 0,
                                                                        0, 1, 0, 0,
                                                                        0, 0, 1, 0,
                                                                        0, 0, 0, 1);

        #endregion constants

        #region numeric functions

        /// <summary>
        /// Factorial function, DON'T FEED ME WITH LARGE NUMBERS !!! (n>10 can be huge)
        /// </summary>
        /// <param name="n"></param>
        /// <returns>The product n * n-1 * n-2 * n-3 * .. * 3 * 2 * 1</returns>
        public static int Factorial(int n)
        {
            if (n == 0)
            {
                return 1;
            }
            if (n < 0) { n = -n; }
            return n * Factorial(n - 1);
        }

        /// <summary>
        /// Binomial function
        /// </summary>
        /// <param name="n"></param>
        /// <param name="k"></param>
        /// <returns>The number of k-tuples of n items</returns>
        public static long Binomial(int n, int k)
        {
            if (n < 0) { n = -n; }
            return Factorial(n) / (Factorial(k) * Factorial(n - k));
        }

        /// <summary>
        /// Raises x to the power of y.
        /// </summary>
        /// <param name="x">The base.</param>
        /// <param name="y">The exponent.</param>
        /// <returns>Returns x raised to the power of y.</returns>
        /// <remarks>This method should be considerably faster than Math.Pow for small y.</remarks>
        public static float Pow(float x, int y)
        {
            var result = 1.0f;
            for (int i = 0; i < y; i++)
            {
                result *= x;
            }
            return result;
        }

        /// <summary>
        /// Solves a quadratic equation a*x^2 + b*x + c for x
        /// </summary>
        /// <param name="a">Coefficient of x^2</param>
        /// <param name="b">Coefficient of x</param>
        /// <param name="c">Constant</param>
        /// <param name="x1">First solution</param>
        /// <param name="x2">Second solution</param>
        /// <returns>Number of solution, 0, 1, 2 or int.MaxValue</returns>
        public int SolveQuadratic(float a, float b, float c, out float x1, out float x2)
        {
            x1 = 0;
            x2 = 0;

            if (a == 0)
            {
                if ((b == 0) && (c == 0))
                {
                    return int.MaxValue;
                }
                else
                {
                    x1 = -c / b;
                    x2 = x1;
                    return 1;
                }
            }
            else
            {
                double D = b * b - 4 * a * c;

                if (D > 0)
                {

                    D = Math.Sqrt(D);
                    x1 = (float)(-b + D) / (2 * a);
                    x2 = (float)(-b - D) / (2 * a);
                    return 2;
                }
                else
                {
                    if (D == 0)
                    {
                        x1 = -b / (2 * a);
                        x2 = x1;
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }

        #endregion numeric functions

        #region range functions


        /// <summary>
        /// Min function
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>Smaller value of the two input parameters</returns>
        public static float Min(float a, float b)
        {
            return a < b ? a : b;
        }

        /// <summary>
        /// Max function
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>Greater value of the two input parameters</returns>
        public static float Max(float a, float b)
        {
            return a > b ? a : b;
        }

        /// <summary>
        /// Modulo function with the property, that the remainder of a division z / d
        /// and z &lt; 0 is positive. For example: zmod(-2, 30) = 28.
        /// </summary>
        /// <param name="z"></param>
        /// <param name="d"></param>
        /// <returns>Remainder of division z / d.</returns>
        public static int Zmod(int z, int d)
        {
            if (z >= d)
                return z % d;
            else if (z < 0)
            {
                int remainder = z % d;
                return remainder == 0 ? 0 : remainder + d;
            }
            else
                return z;
        }

        /// <summary>
        /// Clamp function, clamps a floating point value into the range [min..max]
        /// </summary>
        /// <param name="x"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static float Clamp(float x, float min, float max)
        {
            float minTemp = Min(min, max);
            float maxTemp = Max(min, max);
            return Min(Max(x, minTemp), maxTemp);
        }

        /// <summary>
        /// Clamp function, clamps a floating point value into the range [min..max]
        /// </summary>
        /// <param name="x"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static double Clamp(double x, double min, double max)
        {
            double minTemp = Math.Min(min, max);
            double maxTemp = Math.Max(min, max);
            return Math.Min(Math.Max(x, minTemp), maxTemp);
        }

        /// <summary>
        /// Clamp function, clamps an integer value into the range [min..max]
        /// </summary>
        /// <param name="x"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int Clamp(int x, int min, int max)
        {
            int minTemp = Math.Min(min, max);
            int maxTemp = Math.Max(min, max);
            return Math.Min(Math.Max(x, minTemp), maxTemp);
        }

        /// <summary>
        /// Clamp function, clamps a long value into the range [min..max]
        /// </summary>
        /// <param name="x"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static long Clamp(long x, long min, long max)
        {
            var minTemp = Math.Min(min, max);
            var maxTemp = Math.Max(min, max);
            return Math.Min(Math.Max(x, minTemp), maxTemp);
        }

        /// <summary>
        /// Clamp function, clamps a 2d-vector into the range [min..max]
        /// </summary>
        /// <param name="v"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static Vector2 Clamp(Vector2 v, float min, float max)
        {
            return new Vector2(Clamp(v.X, min, max), Clamp(v.Y, min, max));
        }

        /// <summary>
        /// Clamp function, clamps a 3d-vector into the range [min..max]
        /// </summary>
        /// <param name="v"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static Vector3 Clamp(Vector3 v, float min, float max)
        {
            return new Vector3(Clamp(v.X, min, max), Clamp(v.Y, min, max), Clamp(v.Z, min, max));
        }

        /// <summary>
        /// Clamp function, clamps a 4d-vector into the range [min..max]
        /// </summary>
        /// <param name="v"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static Vector4 Clamp(Vector4 v, float min, float max)
        {
            return new Vector4(Clamp(v.X, min, max), Clamp(v.Y, min, max), Clamp(v.Z, min, max), Clamp(v.W, min, max));
        }

        /// <summary>
        /// Clamp function, clamps a 2d-vector into the range [min..max]
        /// </summary>
        /// <param name="v"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static Vector2 Clamp(Vector2 v, Vector2 min, Vector2 max)
        {
            return new Vector2(Clamp(v.X, min.X, max.X), Clamp(v.Y, min.Y, max.Y));
        }

        /// <summary>
        /// Clamp function, clamps a 3d-vector into the range [min..max]
        /// </summary>
        /// <param name="v"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static Vector3 Clamp(Vector3 v, Vector3 min, Vector3 max)
        {
            return new Vector3(Clamp(v.X, min.X, max.X), Clamp(v.Y, min.Y, max.Y), Clamp(v.Z, min.Z, max.Z));
        }

        /// <summary>
        /// Clamp function, clamps a 4d-vector into the range [min..max]
        /// </summary>
        /// <param name="v"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static Vector4 Clamp(Vector4 v, Vector4 min, Vector4 max)
        {
            return new Vector4(Clamp(v.X, min.X, max.X), Clamp(v.Y, min.Y, max.Y), Clamp(v.Z, min.Z, max.Z), Clamp(v.W, min.W, max.W));
        }

        /// <summary>
        /// Abs function for values, just for completeness
        /// </summary>
        /// <param name="a"></param>
        /// <returns>New value with the absolut value of a</returns>
        public static float Abs(float a)
        {
            return Math.Abs(a);
        }

        /// <summary>
        /// Abs function for 2d-vectors
        /// </summary>
        /// <param name="a"></param>
        /// <returns>New vector with the absolut values of the components of input vector a</returns>
        public static Vector2 Abs(Vector2 a)
        {
            return new Vector2(Math.Abs(a.X), Math.Abs(a.Y));
        }

        /// <summary>
        /// Abs function for 3d-vectors
        /// </summary>
        /// <param name="a"></param>
        /// <returns>New vector with the absolut values of the components of input vector a</returns>
        public static Vector3 Abs(Vector3 a)
        {
            return new Vector3(Math.Abs(a.X), Math.Abs(a.Y), Math.Abs(a.Z));
        }

        /// <summary>
        /// Abs function for 4d-vectors
        /// </summary>
        /// <param name="a"></param>
        /// <returns>New vector with the absolut values of the components of input vector a</returns>
        public static Vector4 Abs(Vector4 a)
        {
            return new Vector4(Math.Abs(a.X), Math.Abs(a.Y), Math.Abs(a.Z), Math.Abs(a.W));
        }

        /// <summary>
        /// This Method can be seen as an inverse of Lerp (in Mode Float). Additionally it provides the infamous Mapping Modes, author: velcrome
        /// </summary>
        /// <param name="Input">Input value to convert</param>
        /// <param name="start">Minimum of input value range</param>
        /// <param name="end">Maximum of input value range</param>
        /// <param name="mode">Defines the behavior of the function if the input value exceeds the destination range 
        /// <see cref="MapMode">TMapMode</see></param>
        /// <returns>Input value mapped from input range into destination range</returns>
        public static float Ratio(float Input, float start, float end, MapMode mode)
        {
            if (end.CompareTo(start) == 0) return 0;

            float range = end - start;
            float ratio = (Input - start) / range;

            if (mode == MapMode.Float) { }
            else if (mode == MapMode.Clamp)
            {
                if (ratio < 0) ratio = 0;
                if (ratio > 1) ratio = 1;
            }
            else
            {
                if (mode == MapMode.Wrap)
                {
                    // includes fix for inconsistent behaviour of old delphi Map 
                    // node when handling integers
                    int rangeCount = (int)Math.Floor(ratio);
                    ratio -= rangeCount;
                }
                else if (mode == MapMode.Mirror)
                {
                    // merke: if you mirror an input twice it is displaced twice the range. same as wrapping twice really
                    int rangeCount = (int)Math.Floor(ratio);
                    rangeCount -= rangeCount & 1; // if uneven, make it even. bitmask of one is same as mod2
                    ratio -= rangeCount;

                    if (ratio > 1) ratio = 2 - ratio; // if on the max side of things now (due to rounding down rangeCount), mirror once against max
                }
            }
            return ratio;
        }

        /// <summary>
        /// The infamous Map function of vvvv for values
        /// </summary>
        /// <param name="Input">Input value to convert</param>
        /// <param name="InMin">Minimum of input value range</param>
        /// <param name="InMax">Maximum of input value range</param>
        /// <param name="OutMin">Minimum of destination value range</param>
        /// <param name="OutMax">Maximum of destination value range</param>
        /// <param name="mode">Defines the behavior of the function if the input value exceeds the destination range 
        /// <see cref="MapMode">TMapMode</see></param>
        /// <returns>Input value mapped from input range into destination range</returns>
        public static float Map(float Input, float InMin, float InMax, float OutMin, float OutMax, MapMode mode)
        {
            float ratio = Ratio(Input, InMin, InMax, mode);
            return Lerp(OutMin, OutMax, ratio);
        }

        /// <summary>
        /// The infamous Map function of vvvv for 2d-vectors and value range bounds
        /// </summary>
        /// <param name="Input">Input value to convert</param>
        /// <param name="InMin">Minimum of input value range</param>
        /// <param name="InMax">Maximum of input value range</param>
        /// <param name="OutMin">Minimum of destination value range</param>
        /// <param name="OutMax">Maximum of destination value range</param>
        /// <param name="mode">Defines the behavior of the function if the input value exceeds the destination range 
        /// <see cref="MapMode">TMapMode</see></param>
        /// <returns>Input vector mapped from input range into destination range</returns>
        public static Vector2 Map(Vector2 Input, float InMin, float InMax, float OutMin, float OutMax, MapMode mode)
        {
            return new Vector2(Map(Input.X, InMin, InMax, OutMin, OutMax, mode),
                                Map(Input.Y, InMin, InMax, OutMin, OutMax, mode));
        }

        /// <summary>
        /// The infamous Map function of vvvv for 3d-vectors and value range bounds
        /// </summary>
        /// <param name="Input">Input value to convert</param>
        /// <param name="InMin">Minimum of input value range</param>
        /// <param name="InMax">Maximum of input value range</param>
        /// <param name="OutMin">Minimum of destination value range</param>
        /// <param name="OutMax">Maximum of destination value range</param>
        /// <param name="mode">Defines the behavior of the function if the input value exceeds the destination range 
        /// <see cref="MapMode">TMapMode</see></param>
        /// <returns>Input vector mapped from input range into destination range</returns>
        public static Vector3 Map(Vector3 Input, float InMin, float InMax, float OutMin, float OutMax, MapMode mode)
        {
            return new Vector3(Map(Input.X, InMin, InMax, OutMin, OutMax, mode),
                                Map(Input.Y, InMin, InMax, OutMin, OutMax, mode),
                                Map(Input.Z, InMin, InMax, OutMin, OutMax, mode));
        }

        /// <summary>
        /// The infamous Map function of vvvv for 4d-vectors and value range bounds
        /// </summary>
        /// <param name="Input">Input value to convert</param>
        /// <param name="InMin">Minimum of input value range</param>
        /// <param name="InMax">Maximum of input value range</param>
        /// <param name="OutMin">Minimum of destination value range</param>
        /// <param name="OutMax">Maximum of destination value range</param>
        /// <param name="mode">Defines the behavior of the function if the input value exceeds the destination range 
        /// <see cref="MapMode">TMapMode</see></param>
        /// <returns>Input vector mapped from input range into destination range</returns>
        public static Vector4 Map(Vector4 Input, float InMin, float InMax, float OutMin, float OutMax, MapMode mode)
        {
            return new Vector4(Map(Input.X, InMin, InMax, OutMin, OutMax, mode),
                                Map(Input.Y, InMin, InMax, OutMin, OutMax, mode),
                                Map(Input.Z, InMin, InMax, OutMin, OutMax, mode),
                                Map(Input.W, InMin, InMax, OutMin, OutMax, mode));
        }

        /// <summary>
        /// The infamous Map function of vvvv for 2d-vectors and range bounds given as vectors
        /// </summary>
        /// <param name="Input">Input value to convert</param>
        /// <param name="InMin">Minimum of input value range</param>
        /// <param name="InMax">Maximum of input value range</param>
        /// <param name="OutMin">Minimum of destination value range</param>
        /// <param name="OutMax">Maximum of destination value range</param>
        /// <param name="mode">Defines the behavior of the function if the input value exceeds the destination range 
        /// <see cref="MapMode">TMapMode</see></param>
        /// <returns>Input vector mapped from input range into destination range</returns>
        public static Vector2 Map(Vector2 Input, Vector2 InMin, Vector2 InMax, Vector2 OutMin, Vector2 OutMax, MapMode mode)
        {
            return new Vector2(Map(Input.X, InMin.X, InMax.X, OutMin.X, OutMax.X, mode),
                                Map(Input.Y, InMin.Y, InMax.Y, OutMin.Y, OutMax.Y, mode));
        }

        /// <summary>
        /// The infamous Map function of vvvv for 3d-vectors and range bounds given as vectors
        /// </summary>
        /// <param name="Input">Input value to convert</param>
        /// <param name="InMin">Minimum of input value range</param>
        /// <param name="InMax">Maximum of input value range</param>
        /// <param name="OutMin">Minimum of destination value range</param>
        /// <param name="OutMax">Maximum of destination value range</param>
        /// <param name="mode">Defines the behavior of the function if the input value exceeds the destination range 
        /// <see cref="MapMode">TMapMode</see></param>
        /// <returns>Input vector mapped from input range into destination range</returns>
        public static Vector3 Map(Vector3 Input, Vector3 InMin, Vector3 InMax, Vector3 OutMin, Vector3 OutMax, MapMode mode)
        {
            return new Vector3(Map(Input.X, InMin.X, InMax.X, OutMin.X, OutMax.X, mode),
                                Map(Input.Y, InMin.Y, InMax.Y, OutMin.Y, OutMax.Y, mode),
                                Map(Input.Z, InMin.Z, InMax.Z, OutMin.Z, OutMax.Z, mode));
        }

        /// <summary>
        /// The infamous Map function of vvvv for 4d-vectors and range bounds given as vectors
        /// </summary>
        /// <param name="Input">Input value to convert</param>
        /// <param name="InMin">Minimum of input value range</param>
        /// <param name="InMax">Maximum of input value range</param>
        /// <param name="OutMin">Minimum of destination value range</param>
        /// <param name="OutMax">Maximum of destination value range</param>
        /// <param name="mode">Defines the behavior of the function if the input value exceeds the destination range 
        /// <see cref="MapMode">TMapMode</see></param>
        /// <returns>Input vector mapped from input range into destination range</returns>
        public static Vector4 Map(Vector4 Input, Vector4 InMin, Vector4 InMax, Vector4 OutMin, Vector4 OutMax, MapMode mode)
        {
            return new Vector4(Map(Input.X, InMin.X, InMax.X, OutMin.X, OutMax.X, mode),
                                Map(Input.Y, InMin.Y, InMax.Y, OutMin.Y, OutMax.Y, mode),
                                Map(Input.Z, InMin.Z, InMax.Z, OutMin.Z, OutMax.Z, mode),
                                Map(Input.W, InMin.W, InMax.W, OutMin.W, OutMax.W, mode));
        }

        #endregion range functions

        #region interpolation

        //Lerp---------------------------------------------------------------------------------------------

        /// <summary>
        /// Linear interpolation (blending) between two values
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="x"></param>
        /// <returns>Linear interpolation between a and b if x in the range ]0..1[ or a if x = 0 or b if x = 1</returns>
        public static float Lerp(float a, float b, float x)
        {
            return a + x * (b - a);
        }

        #endregion
    }
}
