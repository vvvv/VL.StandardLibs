using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using VL.Core;

namespace VL.Lib.Mathematics
{
    public static class RectangleNodes
    {
        public static readonly RectangleF DefaultRect = new RectangleF(-0.5f, -0.5f, 1, 1);

        /// <summary>
        /// Creates the rectangle defined by anchor position and size.
        /// </summary>
        /// <remarks>
        /// Size can be negative.
        /// </remarks>
        public static void Join(ref Vector2 position, ref Vector2 size, RectangleAnchor anchor, out RectangleF output)
        {
            JoinComponentwise(position.X, position.Y, size.X, size.Y, anchor, out output);
        }

        /// <summary>
        /// Creates the rectangle defined by anchor position and size.
        /// </summary>
        /// <remarks>
        /// Size can be negative.
        /// </remarks>
        public static void JoinComponentwise(float positionX, float positionY, float width, float height, RectangleAnchor anchor, out RectangleF output)
        {
            switch (anchor)
            {
                case RectangleAnchor.TopLeft:
                    output = new RectangleF(positionX, positionY, width, height);
                    return;
                case RectangleAnchor.TopCenter:
                    output = new RectangleF(positionX - 0.5f * width, positionY, width, height);
                    return;
                case RectangleAnchor.TopRight:
                    output = new RectangleF(positionX - width, positionY, width, height);
                    return;
                case RectangleAnchor.MiddleLeft:
                    output = new RectangleF(positionX, positionY - 0.5f * height, width, height);
                    return;
                case RectangleAnchor.Center:
                    output = new RectangleF(positionX - 0.5f * width, positionY - 0.5f * height, width, height);
                    return;
                case RectangleAnchor.MiddleRight:
                    output = new RectangleF(positionX - width, positionY - 0.5f * height, width, height);
                    return;
                case RectangleAnchor.BottomLeft:
                    output = new RectangleF(positionX, positionY - height, width, height);
                    return;
                case RectangleAnchor.BottomCenter:
                    output = new RectangleF(positionX - 0.5f * width, positionY - height, width, height);
                    return;
                case RectangleAnchor.BottomRight:
                    output = new RectangleF(positionX - width, positionY - height, width, height);
                    return;
            }

            output = new RectangleF(positionX, positionY, width, height);
        }


        /// <summary>
        /// Gets anchor position and size of the rectangle.
        /// </summary>
        /// <remarks>
        /// Size can be negative.
        /// </remarks>
        public static void Split(ref RectangleF input, RectangleAnchor anchor, out Vector2 position, out Vector2 size)
        {
            float positionX, positionY, width, height;
            SplitComponentwise(ref input, anchor, out positionX, out positionY, out width, out height);
            position = new Vector2(positionX, positionY);
            size = new Vector2(width, height);
        }

        /// <summary>
        /// Gets anchor position and size of the rectangle.
        /// </summary>
        /// <remarks>
        /// Size can be negative.
        /// </remarks>
        public static void SplitComponentwise(ref RectangleF input, RectangleAnchor anchor, out float positionX, out float positionY, out float width, out float height)
        {
            width = input.Width;
            height = input.Height;

            switch (anchor)
            {
                case RectangleAnchor.TopLeft:
                    positionX = input.Left;
                    positionY = input.Top;
                    return;
                case RectangleAnchor.TopCenter:
                    positionX = input.Left + 0.5f * width;
                    positionY = input.Top;
                    return;
                case RectangleAnchor.TopRight:
                    positionX = input.Left + width;
                    positionY = input.Top;
                    return;
                case RectangleAnchor.MiddleLeft:
                    positionX = input.Left;
                    positionY = input.Top + 0.5f * height;
                    return;
                case RectangleAnchor.Center:
                    positionX = input.Left + 0.5f * width;
                    positionY = input.Top + 0.5f * height;
                    return;
                case RectangleAnchor.MiddleRight:
                    positionX = input.Left + width;
                    positionY = input.Top + 0.5f * height;
                    return;
                case RectangleAnchor.BottomLeft:
                    positionX = input.Left;
                    positionY = input.Top + height;
                    return;
                case RectangleAnchor.BottomCenter:
                    positionX = input.Left + 0.5f * width;
                    positionY = input.Top + height;
                    return;
                case RectangleAnchor.BottomRight:
                    positionX = input.Left + width;
                    positionY = input.Top + height;
                    return;
            }

            positionX = input.Left;
            positionY = input.Top;
        }

        /// <summary>
        /// Creates the rectangle spanned by the points.
        /// </summary>
        /// <remarks>
        /// Size will always be positive.
        /// </remarks>
        public static void JoinPoints(ref Vector2 pointA, ref Vector2 pointB, out RectangleF output)
        {
            float left, right, top, bottom;

            if(pointA.X < pointB.X)
            {
                left = pointA.X;
                right = pointB.X;
            }
            else
            {
                left = pointB.X;
                right = pointA.X;
            }

            if(pointA.Y < pointB.Y)
            {
                top = pointA.Y;
                bottom = pointB.Y;
            }
            else
            {
                top = pointB.Y;
                bottom = pointA.Y;
            }

            output = new RectangleF(left, top, right - left, bottom - top);
        }

        public static void Size(ref RectangleF input, out Vector2 size)
        {
            size = new Vector2(input.Width, input.Height);
        }

        /// <summary>
        /// Creates a rectangle defined the coordinates values of the edges.
        /// </summary>
        public static RectangleF FromLRTB(float left, float right, float top, float bottom)
        {
            return new RectangleF(left, top, right - left, bottom - top);
        }

        /// <summary>
        /// Gets the coordinate values of the edges.
        /// </summary>
        public static void Rectangle(ref RectangleF input, out float left, out float right, out float top, out float bottom)
        {
            left = input.Left;
            right = input.Right;
            top = input.Top;
            bottom = input.Bottom;
        }

        //additional properties

        /// <summary>
        /// Center of the upper edge.
        /// </summary>
        public static void TopCenter(ref RectangleF input, out Vector2 topCenter)
        {
            topCenter = new Vector2(input.Left + 0.5f * input.Width, input.Top);
        }

        /// <summary>
        /// Center of the lower edge.
        /// </summary>
        public static void BottomCenter(ref RectangleF input, out Vector2 topCenter)
        {
            topCenter = new Vector2(input.Left + 0.5f * input.Width, input.Bottom);
        }

        /// <summary>
        /// Center of the left edge.
        /// </summary>
        public static void MiddleLeft(ref RectangleF input, out Vector2 topCenter)
        {
            topCenter = new Vector2(input.Left, input.Top + 0.5f * input.Height);
        }

        /// <summary>
        /// Center of the right edge.
        /// </summary>
        public static void MiddleRight(ref RectangleF input, out Vector2 topCenter)
        {
            topCenter = new Vector2(input.Right, input.Top + 0.5f * input.Height);
        }

        /// <summary>
        /// Offsets the edges of the rectangle in each direction away from center. Negative values deflate the rectngle.
        /// </summary>
        public static void Inflate(ref RectangleF input, float left, float right, float up, float down, out RectangleF output)
        {
            output = new RectangleF(
                input.X - left,
                input.Y - up,
                input.Width + right + left,
                input.Height + down + up
            );
        }

        /// <summary>
        /// Offsets the edges of the rectangle horizontally and vertically away from center. Negative values deflate the rectangle.
        /// </summary>
        public static void InflateCentered(ref RectangleF input, float horizonal, float vertical, out RectangleF output)
        {
            Inflate(ref input, horizonal, horizonal, vertical, vertical, out output);
        }

        /// <summary>
        /// Offsets all edges of the rectangle away from center. Negative values deflate the rectangle.
        /// </summary>
        public static void InflateUniform(ref RectangleF input, float offset, out RectangleF output)
        {
            Inflate(ref input, offset, offset, offset, offset, out output);
        }

        /// <summary>
        /// Scales the edges of the rectangle in each direction away from center. Negative values flip the rectangle.
        /// </summary>
        public static void Scale(ref RectangleF input, float left, float right, float up, float down, out RectangleF output)
        {
            var widthH = input.Width * 0.5f;
            var heightH = input.Height * 0.5f;
            var centerX = input.X + widthH;
            var centerY = input.Y + heightH;
            var x = centerX - widthH * left;
            var y = centerY - heightH * up;
            var r = centerX + widthH * right;
            var b = centerY + heightH * down;

            output = new RectangleF(
                x,
                y,
                r - x,
                b - y
            );
        }

        /// <summary>
        /// Scales the edges of the rectangle horizontally and vertically away from center. Negative values flip the rectangle.
        /// </summary>
        public static void ScaleCentered(ref RectangleF input, float horizonal, float vertical, out RectangleF output)
        {
            Scale(ref input, horizonal, horizonal, vertical, vertical, out output);
        }

        /// <summary>
        /// Scales all edges of the rectangle away from center. Negative values flip the rectangle.
        /// </summary>
        public static void ScaleUniform(ref RectangleF input, float scalar, out RectangleF output)
        {
            Scale(ref input, scalar, scalar, scalar, scalar, out output);
        }

        //Obsolete stuff -------------------

        public static RectangleF FromPositionSize(ref Vector2 topLeft, ref Vector2 size)
        {
            return new RectangleF(topLeft.X, topLeft.Y, size.X, size.Y);
        }

        public static RectangleF FromCenterSize(ref Vector2 center, ref Vector2 size)
        {
            return new RectangleF(center.X - size.X * 0.5f, center.Y - size.Y * 0.5f, size.X, size.Y);
        }

        public static void RectanglePositionSize(ref RectangleF input, out float left, out float top, out float width, out float height)
        {
            left = input.Left;
            top = input.Top;
            width = input.Width;
            height = input.Height;
        }

        public static void RectanglePositionSize(ref RectangleF input, out Vector2 position, out Vector2 size)
        {
            position = new Vector2(input.Left, input.Top);
            size = new Vector2(input.Width, input.Height);
        }

        public static void RectangleCenterSize(ref RectangleF input, out Vector2 center, out Vector2 size)
        {
            center = input.Center;
            size = new Vector2(input.Width, input.Height);
        }

        public static RectangleF MultiplyScale(ref RectangleF input, float scalar = 1)
        {
            var scaling = new Vector2(scalar);
            return Scale(ref input, ref scaling);
        }

        public static RectangleF DivideScale(ref RectangleF input, float scalar = 1)
        {
            var scaling = new Vector2(1/scalar);
            return Scale(ref input, ref scaling);
        }

        /// <summary>
        /// Scales the rectangle in horizontal and vertical direction
        /// </summary>
        /// <param name="input"></param>
        /// <param name="scaling"></param>
        /// <returns></returns>
        public static RectangleF Scale(ref RectangleF input, ref Vector2 scaling)
        {
            var result = new RectangleF(input.X, input.Y, input.Width, input.Height);
            result.Inflate(scaling.X, scaling.Y);
            return result;
        }

        /// <summary>
        /// Changes the position of the rectangle
        /// </summary>
        /// <param name="input"></param>
        /// <param name="translation"></param>
        /// <returns></returns>
        public static RectangleF Translate(ref RectangleF input, ref Vector2 translation)
        {
            var result = new RectangleF(input.X, input.Y, input.Width, input.Height);
            result.Offset(translation.X, translation.Y);
            return result;
        }

        public static RectangleF? Union(this IEnumerable<RectangleF?> rects)
        {
            RectangleF? result = default;
            foreach (var item in rects)
                if (item.HasValue)
                {
                    if (result.HasValue)
                        result = RectangleF.Union(result.Value, item.Value);
                    else
                        result = item;
                }
            return result;
        }
        public static RectangleF? Union(this IEnumerable<Vector2> points)
        {
            RectangleF? result = default;
            foreach (var item in points)
            {
                var _ = new RectangleF(item.X, item.Y, 0, 0);
                if (result.HasValue)
                    result = RectangleF.Union(result.Value, _);
                else
                    result = _;
            }
            return result;
        }
    }
}
