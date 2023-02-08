// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Stride.Core.Mathematics;

namespace VL.Lib.Mathematics
{
    /// <summary>
    /// Random functions on commun types.
    /// </summary>
    public static class RandomUtil
    {
        /// <summary>
        /// Gets random <c>float</c> number within range.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="min">Minimum.</param>
        /// <param name="max">Maximum.</param>
        /// <returns>Random <c>float</c> number.</returns>
        public static float NextFloat(this Random random, float min, float max)
        {
            return MathUtil.Lerp(min, max, (float)random.NextDouble());
        }

        /// <summary>
        /// Gets random <c>double</c> number within range.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="min">Minimum.</param>
        /// <param name="max">Maximum.</param>
        /// <returns>Random <c>double</c> number.</returns>
        public static double NextDouble(this Random random, double min, double max)
        {
            return MathUtil.Lerp(min, max, random.NextDouble());
        }

        /// <summary>
        /// Gets random <c>long</c> number.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <returns>Random <c>long</c> number.</returns>
        public static long NextLong(this Random random)
        {
            var buffer = new byte[sizeof(long)];
            random.NextBytes(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }

        /// <summary>
        /// Gets random <c>long</c> number within range.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="min">Minimum.</param>
        /// <param name="max">Maximum.</param>
        /// <returns>Random <c>long</c> number.</returns>
        public static long NextLong(this Random random, long min, long max)
        {
            byte[] buf = new byte[sizeof(long)];
            random.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return (Math.Abs(longRand % (max - min + 1)) + min);
        }

        /// <summary>
        /// Gets random <see cref="Vector2"/> within range.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="min">Minimum.</param>
        /// <param name="max">Maximum.</param>
        /// <returns>Random <see cref="Vector2"/>.</returns>
        public static Vector2 NextVector2(this Random random, Vector2 min, Vector2 max)
        {
            return new Vector2(random.NextFloat(min.X, max.X), random.NextFloat(min.Y, max.Y));
        }

        /// <summary>
        /// Gets random <see cref="Vector3"/> within range.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="min">Minimum.</param>
        /// <param name="max">Maximum.</param>
        /// <returns>Random <see cref="Vector3"/>.</returns>
        public static Vector3 NextVector3(this Random random, Vector3 min, Vector3 max)
        {
            return new Vector3(random.NextFloat(min.X, max.X), random.NextFloat(min.Y, max.Y), random.NextFloat(min.Z, max.Z));
        }

        /// <summary>
        /// Gets random <see cref="Vector4"/> within range.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="min">Minimum.</param>
        /// <param name="max">Maximum.</param>
        /// <returns>Random <see cref="Vector4"/>.</returns>
        public static Vector4 NextVector4(this Random random, Vector4 min, Vector4 max)
        {
            return new Vector4(random.NextFloat(min.X, max.X), random.NextFloat(min.Y, max.Y), random.NextFloat(min.Z, max.Z), random.NextFloat(min.W, max.W));
        }

        /// <summary>
        /// Gets random <see cref="Color4"/> within range.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="min">Minimum.</param>
        /// <param name="max">Maximum.</param>
        /// <returns>Random <see cref="Color4"/>.</returns>
        public static Color4 NextColor4(this Random random, Color4 min, Color4 max)
        {
            return new Color4(random.NextFloat(min.R, max.R), random.NextFloat(min.G, max.G), random.NextFloat(min.B, max.B), random.NextFloat(min.A, max.A));
        }

        /// <summary>
        /// Gets random <see cref="Point"/>.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="min">Minimum.</param>
        /// <param name="max">Maximum.</param>
        /// <returns>Random <see cref="Point"/>.</returns>
        public static Point NextPoint(this Random random, Point min, Point max)
        {
            return new Point(random.Next(min.X, max.X), random.Next(min.Y, max.Y));
        }

        /// <summary>
        /// Gets random <see cref="System.TimeSpan"/>.
        /// </summary>
        /// <param name="random">Current <see cref="System.Random"/>.</param>
        /// <param name="min">Minimum.</param>
        /// <param name="max">Maximum.</param>
        /// <returns>Random <see cref="System.TimeSpan"/>.</returns>
        public static TimeSpan NextTime(this Random random, TimeSpan min, TimeSpan max)
        {
            return TimeSpan.FromTicks(random.NextLong(min.Ticks, max.Ticks));
        }
    }
}