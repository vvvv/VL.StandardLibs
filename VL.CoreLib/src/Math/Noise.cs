using System;
using System.Collections.Generic;
using System.Linq;
using VL.Core;
using Stride.Core.Mathematics;
  
namespace VL.Lib.Mathematics
{
    public static class Noise
    {
        [ThreadStatic]
        private static Random FRandomGenerator;

        private static Random RandomGenerator => FRandomGenerator ?? (FRandomGenerator = new Random());

        public static int Random(int from, int to = 1)
        {
            return RandomGenerator.Next(from, to);
        }

        /// <summary>
        /// Returns a random number between 0.0 and 1.0.
        /// </summary>
        /// <returns>A random number between 0.0 and 1.0.</returns>
        public static float Random(float from, float to = 1)
        {
            return (float)RandomGenerator.NextDouble() * (to - from) + from;
        }

        public static double Random(double from, double to = 1)
        {
            return RandomGenerator.NextDouble() * (to - from) + from;
        }

        public static Vector2 Random(Vector2 from, Vector2 to)
        {
            return RandomGenerator.NextVector2(from, to);
        }

        public static Vector3 Random(Vector3 from, Vector3 to)
        {
            return RandomGenerator.NextVector3(from, to);
        }

        public static Vector4 Random(Vector4 from, Vector4 to)
        {
            return RandomGenerator.NextVector4(from, to);
        }

        public static Color4 Random(Color4 from, Color4 to)
        {
            return RandomGenerator.NextColor4(from, to);
        }
    }
}
