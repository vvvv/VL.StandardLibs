using SkiaSharp;
using System;
using VL.Lib.Mathematics;
using VL.UI.Core;

namespace VL.Skia
{
    static class CallerInfoExtensions
    {
        /// <summary>
        /// Adjust the space for upstream - based on the downstream tranformation.
        /// Can be used by cameras or normal object to world space transformations.
        /// Adjust the camera further downstream to make it work as expected.
        /// </summary>
        public static CallerInfo PushTransformation(this CallerInfo callerInfo, SKMatrix relative)
        {
            SKMatrix target = callerInfo.Transformation;
            return callerInfo with { Transformation = target.PreConcat(relative) };
        }

        /// <summary>
        /// Setup a new viewport.
        /// If we are already in a viewport this places the new viewport inside the downstream viewport.
        /// </summary>
        public static CallerInfo InViewport(this CallerInfo callerInfo, SKRect viewportBoundsInWindowPix, Func<CallerInfo, SKMatrix> getTransformation)
        {
            var result = callerInfo with { ViewportBounds = viewportBoundsInWindowPix };

            // we need to reset the transformation to reflect the new viewport
            // in the end its only the transformation that influences the rendering
            return result.WithTransformation(getTransformation(result));
        }

        /// <summary>
        /// Applies the space by resetting the Transformation.
        /// Further upstream you may use cameras and other transformations and thus invent your own space.
        /// </summary>
        internal static SKMatrix GetWithinSpaceTransformation(SKRect viewportBounds, Sizing sizing = Sizing.ManualSize, 
            float width = 0, float height = 2, RectangleAnchor origin = RectangleAnchor.Center)
        {
            SKMatrix transformation = SKMatrix.CreateIdentity();
            switch (sizing)
            {
                case Sizing.ManualSize:
                    if (width == 0)
                    {
                        if (height == 0)
                            return GetWithinSpaceTransformation(viewportBounds, Sizing.Pixels, 0, 0, origin);

                        var scale = viewportBounds.Height / height;
                        transformation.ScaleX = scale;
                        transformation.ScaleY = scale;
                    }
                    else
                    {
                        if (height == 0)
                        {
                            var scale = viewportBounds.Width / width;
                            transformation.ScaleX = scale;
                            transformation.ScaleY = scale;
                        }
                        else
                        {
                            transformation.ScaleX = viewportBounds.Width / width;
                            transformation.ScaleY = viewportBounds.Height / height;
                        }
                    }
                    break;
                case Sizing.Pixels:
                    transformation.ScaleX = 100f;
                    transformation.ScaleY = 100f;
                    break;
                case Sizing.DIP:
                    transformation.ScaleX = DIPHelpers.DIPFactor() * 100f;
                    transformation.ScaleY = DIPHelpers.DIPFactor() * 100f;
                    break;
                default:
                    throw new NotImplementedException();
            }

            switch (origin.Horizontal())
            {
                case RectangleAnchorHorizontal.Left:
                    transformation.TransX = viewportBounds.Left;
                    break;
                case RectangleAnchorHorizontal.Center:
                    transformation.TransX = (viewportBounds.Left + viewportBounds.Right) / 2;
                    break;
                case RectangleAnchorHorizontal.Right:
                    transformation.TransX = viewportBounds.Right;
                    break;
                default:
                    break;
            }

            switch (origin.Vertical())
            {
                case RectangleAnchorVertical.Top:
                    transformation.TransY = viewportBounds.Top;
                    break;
                case RectangleAnchorVertical.Center:
                    transformation.TransY = (viewportBounds.Top + viewportBounds.Bottom) / 2;
                    break;
                case RectangleAnchorVertical.Bottom:
                    transformation.TransY = viewportBounds.Bottom;
                    break;
                default:
                    break;
            }

            return transformation;
        }

        internal static SKMatrix GetWithinCommonSpaceTransformation(SKRect viewportBounds, CommonSpace space)
        {
            switch (space)
            {
                case CommonSpace.Normalized:
                    return GetWithinSpaceTransformation(viewportBounds, Sizing.ManualSize, 0, 2, RectangleAnchor.Center);
                case CommonSpace.DIP:
                    return GetWithinSpaceTransformation(viewportBounds, Sizing.DIP, 0, 2, RectangleAnchor.Center);
                case CommonSpace.DIPTopLeft:
                    return GetWithinSpaceTransformation(viewportBounds, Sizing.DIP, 0, 2, RectangleAnchor.TopLeft);
                //case CommonSpace.PixelCentered:
                //    return GetWithinSpaceTransformation(Sizing.Pixels, 0, 2, RectangleAnchor.Center);
                case CommonSpace.PixelTopLeft:
                    return GetWithinSpaceTransformation(viewportBounds, Sizing.Pixels, 0, 2, RectangleAnchor.TopLeft);
                //case CommonSpace.HDCentered:
                //    return GetWithinSpaceTransformation(Sizing.ManualSize, 0, 10.80f, RectangleAnchor.Center);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
