using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VL.Core;
using Stride.Core.Mathematics;

namespace VL.Lib.Mathematics
{
    public static class BoxExtensions
    {
        /// <summary>
        /// Creates a Box from center position and size vector. Initially, the Box is axis-aligned box, but it can be rotated and transformed later
        /// </summary>
        /// <param name="center"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static OrientedBoundingBox CreateCenterSize(ref Vector3 center, ref Vector3 size)
        {
            return new OrientedBoundingBox() { Extents = size * 0.5f, Transformation = Matrix.Translation(center) };
        }

        /// <summary>
        /// Creates a Box from extends (half size for each axis) and transformation
        /// </summary>
        /// <param name="extends"></param>
        /// <param name="transformation"></param>
        /// <returns></returns>
        public static OrientedBoundingBox BoxJoin(ref Vector3 extends, ref Matrix transformation)
        {
            return new OrientedBoundingBox() { Extents = extends, Transformation = transformation };
        }

        public static OrientedBoundingBox MultiplyScale(ref OrientedBoundingBox input, float scalar = 1)
        {
            return new OrientedBoundingBox() { Extents = input.Extents * scalar, Transformation = input.Transformation };
        }

        public static OrientedBoundingBox DivideScale(ref OrientedBoundingBox input, float scalar = 1)
        {
            return new OrientedBoundingBox() { Extents = input.Extents / scalar, Transformation = input.Transformation };
        }

        /// <summary>
        /// Scales the Box by scaling its Extents without affecting the Transformation matrix. By keeping Transformation matrix scaling-free, the collision detection methods will be more accurate
        /// </summary>
        /// <param name="input"></param>
        /// <param name="scaling"></param>
        /// <returns></returns>
        public static OrientedBoundingBox Scale(ref OrientedBoundingBox input, ref Vector3 scaling)
        {
            return new OrientedBoundingBox() { Extents = input.Extents * scaling, Transformation = input.Transformation };
        }

        /// <summary>
        /// Translates the Box to a new position using a translation vector
        /// </summary>
        /// <param name="input"></param>
        /// <param name="translation"></param>
        /// <returns></returns>
        public static OrientedBoundingBox Translate(ref OrientedBoundingBox input, ref Vector3 translation)
        {
            var result = new OrientedBoundingBox() { Extents = input.Extents, Transformation = input.Transformation };
            result.Translate(ref translation);
            return result;
        }

        /// <summary>
        /// Transforms this Box using a transformation matrix. While any kind of transformation can be applied, it is recommended to apply scaling using scale method instead, which scales the Extents and keeps the Transformation matrix for rotation only, and that preserves collision detection accuracy
        /// </summary>
        /// <param name="input"></param>
        /// <param name="transformation"></param>
        /// <returns></returns>
        public static OrientedBoundingBox Transform(ref OrientedBoundingBox input, ref Matrix transformation)
        {
            var result = new OrientedBoundingBox() { Extents = input.Extents, Transformation = input.Transformation };
            result.Transform(ref transformation);
            return result;
        }
    }
}
