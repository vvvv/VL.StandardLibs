using SkiaSharp;
using System;
using System.Threading;

namespace VL.Skia
{
    public struct RenderInfo
    {
        public static readonly RenderInfo Default = new RenderInfo()
        {
            Transformation = SKMatrix.CreateIdentity(),
            Paint = new SKPaint(),
            Caller = CallerInfo.Default
        };

        public RenderInfo(SKMatrix transformation, SKPaint paint, CallerInfo caller)
        {
            Transformation = transformation;
            Paint = paint;
            Caller = caller;
        }

        public SKMatrix Transformation { get; internal set; }
        public SKPaint Paint { get; internal set; }
        public CallerInfo Caller { get; internal set; }

        public void Split(
            out SKCanvas canvas,
            out SKPaint paint,
            out SKMatrix transformation,
            out CallerInfo caller)
        {
            canvas = Caller.Canvas;
            paint = Paint;
            transformation = Transformation;
            caller = Caller;
        }

        public RenderInfo WithPaint(SKPaint paint)
        {
            return new RenderInfo()
            {
                Transformation = Transformation,
                Paint = paint,
                Caller = Caller
            };
        }
    }
}
