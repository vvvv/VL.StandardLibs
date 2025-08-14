#nullable enable

using System;
using System.Drawing;
using System.Drawing.Imaging;
using SkiaSharp;

namespace VL.Skia;

internal sealed class SoftwareSkiaRenderer : ISkiaRenderer
{
    private Bitmap? bitmap;
    private bool disposed;
    private readonly RenderStopwatch stopwatch = new();

    public float RenderTime => stopwatch.RenderTime;

    public CallerInfo CallerInfo { get; private set; } = CallerInfo.Default;

    public void Render(nint hwnd, int width, int height, bool vsync, Action<CallerInfo> renderAction, Graphics? gdiTarget = null)
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(SoftwareSkiaRenderer));
        if (width <= 0 || height <= 0 || gdiTarget is null)
            return;

        stopwatch.StartRender();
        try
        {
            var info = EnsureBitmap(width, height);
            if (bitmap is null)
                return;

            var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            try
            {
                using var surface = SKSurface.Create(info, data.Scan0, data.Stride);
                var canvas = surface.Canvas;
                CallerInfo = CallerInfo.InRenderer(info.Width, info.Height, canvas, null);
                using (new SKAutoCanvasRestore(canvas, true))
                {
                    renderAction(CallerInfo);
                }
                canvas.Flush();
            }
            finally
            {
                bitmap.UnlockBits(data);
            }

            gdiTarget.DrawImage(bitmap, 0, 0);
        }
        finally
        {
            stopwatch.EndRender();
        }
    }

    private SKImageInfo EnsureBitmap(int width, int height)
    {
        var info = new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
        if (bitmap == null || bitmap.Width != width || bitmap.Height != height)
        {
            bitmap?.Dispose();
            if (width > 0 && height > 0)
                bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        }
        return info;
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        bitmap?.Dispose();
        bitmap = null;
    }
}
