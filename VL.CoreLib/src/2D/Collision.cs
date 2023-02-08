using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using VL.Core;

namespace VL.Lib.Mathematics
{
    /// <summary>
    /// 
    /// This class is organized like the <see cref="Stride.Core.Mathematics.CollisionHelper"/> class.
    /// So that the least complex objects will have the most methods in most cases. 
    /// Note that not all shapes exist at this time and not all shapes have a corresponding struct. 
    /// Only the objects that have a corresponding struct should come first in naming and in parameter order.
    /// The order of complexity is as follows:
    ///     
    /// 1. Point
    /// 2. Circle
    /// 3. Rectangle
    /// 
    /// </summary>
    public static class Collision2D
    {
        //circle---------------------------------------------

        /// <summary>
        /// Checks whether the circle contains the point
        /// </summary>
        /// <param name="circle"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool CircleContainsPoint(ref Circle circle, ref Vector2 point)
        {
            float distanceSquared;
            Vector2.DistanceSquared(ref circle.Center, ref point, out distanceSquared);

            return distanceSquared <= circle.RadiusSquared;
        }

        /// <summary>
        /// Checks whether the circles intersect
        /// </summary>
        /// <param name="circle1"></param>
        /// <param name="circle2"></param>
        /// <returns></returns>
        public static bool CircleIntersectsCircle(ref Circle circle1, ref Circle circle2)
        {
            float distanceSquared;
            Vector2.DistanceSquared(ref circle1.Center, ref circle2.Center, out distanceSquared);
            var radiusSum = circle1.Radius + circle2.Radius;
            return distanceSquared <= radiusSum * radiusSum;
        }

        /// <summary>
        /// Checks whether Circle 1 contains Circle 2
        /// </summary>
        /// <param name="circle1"></param>
        /// <param name="circle2"></param>
        /// <returns></returns>
        public static bool CircleContainsCircle(ref Circle circle1, ref Circle circle2)
        {
            if (circle2.Radius > circle1.Radius)
                return false;

            float distanceSquared;
            Vector2.DistanceSquared(ref circle1.Center, ref circle2.Center, out distanceSquared);
            var radiusDiff = circle1.Radius - circle2.Radius;
            return distanceSquared <= radiusDiff * radiusDiff;
        }

        /// <summary>
        /// Checks whether the circle and the rect intersect
        /// </summary>
        /// <param name="circle"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        public static bool CircleIntersectsRect(ref Circle circle, ref RectangleF rect)
        {
            var circleDistanceX = Math.Abs(circle.Center.X - rect.Center.X);
            var circleDistanceY = Math.Abs(circle.Center.Y - rect.Center.Y);

            var halfWidth = rect.Width * 0.5f;
            var halfHeight = rect.Height * 0.5f;

            if (circleDistanceX > (halfWidth + circle.Radius)) { return false; }
            if (circleDistanceY > (halfHeight + circle.Radius)) { return false; }

            if (circleDistanceX <= halfWidth) { return true; }
            if (circleDistanceY <= halfHeight) { return true; }

            var distX = circleDistanceX - halfWidth;
            var distY = circleDistanceY - halfHeight;
            var cornerDistanceSquared = distX * distX + distY * distY;

            return cornerDistanceSquared <= circle.RadiusSquared;
        }

        //rectangle----------------------------------

        /// <summary>
        /// Checks whether the rectangle contains the point
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="point"></param>
        /// <param name="result"></param>
        public static void RectContainsPoint(ref RectangleF rectangle, ref Vector2 point, out bool result)
        {
            rectangle.Contains(ref point, out result);
        }

        /// <summary>
        /// Checks whether the rectangles intersect
        /// </summary>
        /// <param name="rectangle1"></param>
        /// <param name="rectangle2"></param>
        /// <param name="result"></param>
        public static void RectIntersectsRect(ref RectangleF rectangle1, ref RectangleF rectangle2, out bool result)
        {
            rectangle1.Intersects(ref rectangle2, out result);
        }

        /// <summary>
        /// Checks whether Rectangle 1 contains Rectangle 2
        /// </summary>
        /// <param name="rectangle1"></param>
        /// <param name="rectangle2"></param>
        /// <param name="result"></param>
        public static void RectContainsRect(ref RectangleF rectangle1, ref RectangleF rectangle2, out bool result)
        {
            rectangle1.Contains(ref rectangle2, out result);
        }
        
    }
}
