using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Skia;
using Avalonia.Visuals.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using VL.Skia;

namespace VL.AvaloniaUI
{
    static class Utils
    {
        public static T UseCustomSkia<T>(this T builder) where T : AppBuilderBase<T>, new()
        {
            return builder.With(new SkiaOptions() { CustomGpuFactory = () => new SkiaGpu() })
                .UseSkia()
                .AfterPlatformServicesSetup(_ =>
                {
                    var renderInterface = new PlatformRenderInterface();
                    AvaloniaLocator.CurrentMutable
                        .Bind<IPlatformRenderInterface>().ToConstant(renderInterface);
                });
        }

        public static Point ToPoint(this Stride.Core.Mathematics.Vector2 v) => new Point(v.X, v.Y);

        sealed class SkiaGpu : ISkiaGpu
        {
            public ISkiaGpuRenderTarget TryCreateRenderTarget(IEnumerable<object> surfaces)
            {
                // Shouldn't get called
                throw new NotSupportedException();
            }

            public ISkiaSurface TryCreateSurface(PixelSize size, ISkiaGpuRenderSession session)
            {
                // Shouldn't get called
                throw new NotSupportedException();
            }
        }

        sealed class PlatformRenderInterface : IPlatformRenderInterface, IOpenGlAwarePlatformRenderInterface
        {
            private readonly IPlatformRenderInterface platformRenderInterface;
            private readonly IOpenGlAwarePlatformRenderInterface glAwarePlatformRenderInterface;

            public PlatformRenderInterface()
            {
                platformRenderInterface = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
                glAwarePlatformRenderInterface = AvaloniaLocator.Current.GetService<IOpenGlAwarePlatformRenderInterface>();
            }

            public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
            {
                foreach (var s in surfaces)
                    if (s is CallerInfo c)
                        return new RenderTarget(c);

                return platformRenderInterface.CreateRenderTarget(surfaces);
            }

            public bool SupportsIndividualRoundRects => platformRenderInterface.SupportsIndividualRoundRects;

            public AlphaFormat DefaultAlphaFormat => platformRenderInterface.DefaultAlphaFormat;

            public PixelFormat DefaultPixelFormat => platformRenderInterface.DefaultPixelFormat;

            public IGeometryImpl CreateCombinedGeometry(GeometryCombineMode combineMode, Geometry g1, Geometry g2)
            {
                return platformRenderInterface.CreateCombinedGeometry(combineMode, g1, g2);
            }

            public IGeometryImpl CreateEllipseGeometry(Rect rect)
            {
                return platformRenderInterface.CreateEllipseGeometry(rect);
            }

            public IFormattedTextImpl CreateFormattedText(string text, Typeface typeface, double fontSize, TextAlignment textAlignment, TextWrapping wrapping, Size constraint, IReadOnlyList<FormattedTextStyleSpan> spans)
            {
                return platformRenderInterface.CreateFormattedText(text, typeface, fontSize, textAlignment, wrapping, constraint, spans);
            }

            public IGeometryImpl CreateGeometryGroup(FillRule fillRule, IReadOnlyList<Geometry> children)
            {
                return platformRenderInterface.CreateGeometryGroup(fillRule, children);
            }

            public IGlyphRunImpl CreateGlyphRun(GlyphRun glyphRun, out double width)
            {
                return platformRenderInterface.CreateGlyphRun(glyphRun, out width);
            }

            public IGeometryImpl CreateLineGeometry(Point p1, Point p2)
            {
                return platformRenderInterface.CreateLineGeometry(p1, p2);
            }

            public IOpenGlBitmapImpl CreateOpenGlBitmap(PixelSize size, Vector dpi)
            {
                return glAwarePlatformRenderInterface.CreateOpenGlBitmap(size, dpi);
            }

            public IGeometryImpl CreateRectangleGeometry(Rect rect)
            {
                return platformRenderInterface.CreateRectangleGeometry(rect);
            }

            public IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi)
            {
                return platformRenderInterface.CreateRenderTargetBitmap(size, dpi);
            }

            public IStreamGeometryImpl CreateStreamGeometry()
            {
                return platformRenderInterface.CreateStreamGeometry();
            }

            public IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat format, AlphaFormat alphaFormat)
            {
                return platformRenderInterface.CreateWriteableBitmap(size, dpi, format, alphaFormat);
            }

            public IBitmapImpl LoadBitmap(string fileName)
            {
                return platformRenderInterface.LoadBitmap(fileName);
            }

            public IBitmapImpl LoadBitmap(Stream stream)
            {
                return platformRenderInterface.LoadBitmap(stream);
            }

            public IBitmapImpl LoadBitmap(PixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size, Vector dpi, int stride)
            {
                return platformRenderInterface.LoadBitmap(format, alphaFormat, data, size, dpi, stride);
            }

            public IBitmapImpl LoadBitmapToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
            {
                return platformRenderInterface.LoadBitmapToHeight(stream, height, interpolationMode);
            }

            public IBitmapImpl LoadBitmapToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
            {
                return platformRenderInterface.LoadBitmapToWidth(stream, width, interpolationMode);
            }

            public IWriteableBitmapImpl LoadWriteableBitmap(string fileName)
            {
                return platformRenderInterface.LoadWriteableBitmap(fileName);
            }

            public IWriteableBitmapImpl LoadWriteableBitmap(Stream stream)
            {
                return platformRenderInterface.LoadWriteableBitmap(stream);
            }

            public IWriteableBitmapImpl LoadWriteableBitmapToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
            {
                return platformRenderInterface.LoadWriteableBitmapToHeight(stream, height, interpolationMode);
            }

            public IWriteableBitmapImpl LoadWriteableBitmapToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
            {
                return platformRenderInterface.LoadWriteableBitmapToWidth(stream, width, interpolationMode);
            }

            public IBitmapImpl ResizeBitmap(IBitmapImpl bitmapImpl, PixelSize destinationSize, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
            {
                return platformRenderInterface.ResizeBitmap(bitmapImpl, destinationSize, interpolationMode);
            }
        }

        sealed class RenderTarget : IRenderTarget
        {
            private readonly CallerInfo callerInfo;

            public RenderTarget(CallerInfo callerInfo)
            {
                this.callerInfo = callerInfo;
            }

            public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
            {
                return Avalonia.Skia.Helpers.DrawingContextHelper.WrapSkiaCanvas(callerInfo.Canvas, new Vector(96, 96), visualBrushRenderer);
            }

            public void Dispose()
            {
            }
        }
    }
}
