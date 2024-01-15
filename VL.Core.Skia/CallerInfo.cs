using SkiaSharp;
using System;
using VL.UI.Core;

namespace VL.Skia
{
    /// <summary>
    /// Information from downstream that gets handed over to upstream on render and on notify. Immutable
    /// </summary>
    public record class CallerInfo
    {
        public static SKMatrix Identity = SKMatrix.CreateIdentity();
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

        public static float DIPFactor => DIPHelpers.DIPFactor();

        /// <summary>
        /// Renderers call this to set up the caller info
        /// </summary>
        public static CallerInfo InRenderer(float width, float height, SKCanvas canvas, GRContext context)
        {
            return new CallerInfo(context, canvas, Identity, new SKRect(0, 0, width, height), null);
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
        /// Use this to implement semantics-like features.
        /// With the delegate you can influence all rendering nodes upstream.
        /// </summary>
        public CallerInfo WithRenderPaintHack(Func<object, object> renderPaintHack)
            => this with { RenderInfoHack = renderPaintHack };

    }
}
