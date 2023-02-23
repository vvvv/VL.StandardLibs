using SkiaSharp;
using System;

namespace VL.Skia
{
    static public class SKMatrixHelpers
    {
        public static SKMatrix Multiply(this SKMatrix input, ref SKMatrix input2) => input.PostConcat(input2);

        public static SKMatrix SetScaleX(this SKMatrix matrix, float value)
        {
            matrix.ScaleX = value;
            return matrix;
        }
        public static SKMatrix SetSkewX(this SKMatrix matrix, float value)
        {
            matrix.SkewX = value;
            return matrix;
        }
        public static SKMatrix SetTransX(this SKMatrix matrix, float value)
        {
            matrix.TransX = value;
            return matrix;
        }
        public static SKMatrix SetSkewY(this SKMatrix matrix, float value)
        {
            matrix.SkewY = value;
            return matrix;
        }
        public static SKMatrix SetScaleY(this SKMatrix matrix, float value)
        {
            matrix.ScaleX = value;
            return matrix;
        }
        public static SKMatrix SetTransY(this SKMatrix matrix, float value)
        {
            matrix.TransY = value;
            return matrix;
        }
        public static SKMatrix SetPersp0(this SKMatrix matrix, float value)
        {
            matrix.Persp0 = value;
            return matrix;
        }
        public static SKMatrix SetPersp1(this SKMatrix matrix, float value)
        {
            matrix.Persp1 = value;
            return matrix;
        }
        public static SKMatrix SetPersp2(this SKMatrix matrix, float value)
        {
            matrix.Persp2 = value;
            return matrix;
        }
        public static SKMatrix MakeSKMatrix(float ScaleX, float SkewX, float TransX, float SkewY, float ScaleY, float TransY, float Persp0, float Persp1, float Persp2)
        {
            SKMatrix matrix = SKMatrix.CreateIdentity();
            matrix.ScaleX = ScaleX;
            matrix.SkewX = SkewX;
            matrix.TransX = TransX;
            matrix.SkewY = SkewY;
            matrix.ScaleY = ScaleY;
            matrix.TransY = TransY;
            matrix.Persp0 = Persp0;
            matrix.Persp1 = Persp1;
            matrix.Persp2 = Persp2;

            return matrix;
        }
    }

    //public static class PathHelpers
    //{
    //    public static void DrawPath(SKCanvas canvas, out SKCanvas canvasOut, SKPaint paint, IResourceProvider<SharpDX.Direct2D1.PathGeometry> path, bool enabled)
    //    {
    //        canvasOut = canvas;
    //        if (!enabled)
    //            return;

    //        path.Using((p) =>
    //        {
    //            using (var skiaPath = SkiaPathConverter.FromDirect2DPath(p))
    //            {
    //                canvas.DrawPath(skiaPath, paint);
    //            }
    //        });
    //    }

    //    public static void DrawPath(SKCanvas canvas, out SKCanvas canvasOut, SKPaint paint, SharpDX.Direct2D1.PathGeometry d2DPath, bool enabled)
    //    {
    //        canvasOut = canvas;
    //        if (!enabled)
    //            return;

    //        using (var path = SkiaPathConverter.FromDirect2DPath(d2DPath))
    //        {
    //            canvas.DrawPath(path, paint);
    //        }
    //    }

    //    public static void DrawPath(SKCanvas canvas, out SKCanvas canvasOut, SKPaint paint, VL.Geometry.PathMessage pathMessage, bool enabled)
    //    {
    //        canvasOut = canvas;
    //        if (!enabled)
    //            return;

    //        using (var path = SkiaPathConverter.FromPathMessage(pathMessage))
    //        {
    //            canvas.DrawPath(path, paint);
    //        }
    //    }
    //}

    static public class SkiaStaticDefaults
    {
        //public static readonly SKSurface DefaultSurface = SKSurface.Create(SKImageInfo.Empty);

        //public static readonly SKBitmap DefaultBitmap = InitDefaultBitmap();

        //private static SKBitmap InitDefaultBitmap()
        //{
        //    SKBitmap bitmap = new SKBitmap();
        //    //bitmap.SetImmutable();
        //    return bitmap;
        //}

        public static readonly ILayer DefaultLayer = new LinkedLayerBase();
        public static SKMatrix IdentityMatrix = SKMatrix.CreateIdentity();
    }


    public static class Helpers
    {
        //public static SKMatrix SafeViewMatrix(this IRenderer renderer)
        //    => renderer?.ViewMatrix ?? Helpers.IdentityMatrix;

        public static void SafeDispose(this SKObject input)
        {
            if (input != null && input.Handle != IntPtr.Zero)
                input.Dispose();
        }       
    }
}
