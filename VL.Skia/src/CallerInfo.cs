using SkiaSharp;
using System;
using System.Drawing;
using VL.Lib.Mathematics;

namespace VL.Skia
{
    /// <summary>
    /// Information from downstream that gets handed over to upstream on render and on notify. Immutable
    /// </summary>
    public record class CallerInfo
    {
        public static SKMatrix Identity = SKMatrix.MakeIdentity();
        public static readonly CallerInfo Default = new CallerInfo(null, new SKCanvas(new SKBitmap()),
            Identity, new SKRect(0, 0, 1920, 1080), null);

        public GRContext GRContext { get; init; }
        public SKCanvas Canvas { get; init; }
        public SKMatrix Transformation { get; init; }
        public SKRect ViewportBounds { get; init; }
        public Func<object, object> RenderInfoHack { get; init; }
        public bool IsTooltip { get; init; }

        internal CallerInfo(GRContext context, SKCanvas canvas, SKMatrix transformation, SKRect viewportBounds,
            Func<object, object> renderInfoHack)
        {
            GRContext = context;
            Canvas = canvas;
            Transformation = transformation;
            ViewportBounds = viewportBounds;
            RenderInfoHack = renderInfoHack;
        }

        static float FDIPFactor = -1;
        public static float DIPFactor
        {
            get
            {
                if (FDIPFactor == -1)
                    using (var g = Graphics.FromHwnd(IntPtr.Zero))
                        FDIPFactor = g.DpiX / 96;
                return FDIPFactor;
            }
        }

        /// <summary>
        /// Renderers call this to set up the caller info
        /// </summary>
        public static CallerInfo InRenderer(float width, float height, SKCanvas canvas, GRContext context)
        {
            return new CallerInfo(context, canvas, Identity, new SKRect(0, 0, width, height), null);
        }

        /// <summary>
        /// Adjust the space for upstream - based on the downstream tranformation.
        /// Can be used by cameras or normal object to world space transformations.
        /// Adjust the camera further downstream to make it work as expected.
        /// </summary>
        internal CallerInfo PushTransformation(SKMatrix relative)
        {
            SKMatrix target = Transformation;
            SKMatrix.PreConcat(ref target, ref relative);
            return this with { Transformation = target };
        }

        public CallerInfo WithTransformation(SKMatrix transformation)
        {
            return this with { Transformation = transformation };
        }

        public CallerInfo WithCanvas(SKCanvas canvas)
        {
            return this with { Canvas = canvas };
        }

        public CallerInfo WithGRContext(GRContext context)
        {
            return this with { GRContext = context };
        }

        public CallerInfo AsTooltip => this with { IsTooltip = true };

        /// <summary>
        /// Setup a new viewport.
        /// If we are already in a viewport this places the new viewport inside the downstream viewport.
        /// </summary>
        internal CallerInfo InViewport(SKRect viewportBoundsInWindowPix, Func<CallerInfo, SKMatrix> getTransformation)
        {
            var result = this with { ViewportBounds = viewportBoundsInWindowPix };

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
            SKMatrix transformation = SKMatrix.MakeIdentity();
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
                    transformation.ScaleX = DIPFactor*100f;
                    transformation.ScaleY = DIPFactor*100f;
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

        /// <summary>
        /// Use this to implement semantics-like features.
        /// With the delegate you can influence all rendering nodes upstream.
        /// </summary>
        public CallerInfo WithRenderPaintHack(Func<object, object> renderPaintHack)
            => this with { RenderInfoHack = renderPaintHack };

    }
}
