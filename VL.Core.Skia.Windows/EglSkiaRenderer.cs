#nullable enable

using System;
using System.Threading;
using SkiaSharp;
using Stride.Core.Mathematics;
using VL.Core;
using VL.Skia.Egl;
using Microsoft.Extensions.Logging;
using System.Drawing;

namespace VL.Skia;

internal sealed class EglSkiaRenderer : ISkiaRenderer
{
    private readonly RenderContextProvider renderContextProvider;
    private readonly RenderStopwatch stopwatch = new();
    private RenderContext? lastRenderContext;
    private EglSurface? eglSurface;
    private SKSurface? surface;
    private Int2 surfaceSize;
    private SKCanvas? canvas;
    private bool? lastSetVSync;
    private bool disposed;
    private float lastScaling;

    public EglSkiaRenderer(RenderContextProvider renderContextProvider)
    {
        this.renderContextProvider = renderContextProvider ?? throw new ArgumentNullException(nameof(renderContextProvider));
    }

    public float RenderTime => stopwatch.RenderTime;

    public CallerInfo CallerInfo { get; private set; } = CallerInfo.Default;

    public void Render(nint hwnd, int width, int height, float scaling, bool vsync, Action<CallerInfo> renderAction, Graphics? gdiTarget = null)
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(EglSkiaRenderer));
        if (hwnd == 0 || width <= 0 || height <= 0)
            return;

        var renderContext = renderContextProvider.GetRenderContext();
        if (renderContext != lastRenderContext)
        {
            DestroySurface();
            lastRenderContext = renderContext;
            lastSetVSync = default;
        }

        stopwatch.StartRender();
        try
        {
            PaintCore(renderContext, hwnd, width, height, scaling, vsync, renderAction);
        }
        catch (EglException)
        {
            DestroySurface();
            throw;
        }
        finally
        {
            stopwatch.EndRender();
        }
    }

    private void PaintCore(RenderContext renderContext, nint hwnd, int width, int height, float scaling, bool vsync, Action<CallerInfo> renderAction)
    {
        var eglContext = renderContext.EglContext;

        if (eglSurface is null || surfaceSize.X != width || surfaceSize.Y != height)
        {
            using var _1 = eglContext.MakeCurrent(forRendering: false);

            DestroySurface();

            surfaceSize = new Int2(width, height);
            eglSurface = eglContext.CreatePlatformWindowSurface(hwnd, width, height);
            lastSetVSync = default;
        }

        if (eglSurface is null)
            return;

        using var _ = renderContext.MakeCurrent(forRendering: true, eglSurface);

        if (surface is null)
        {
            surface = CreateSkSurface(renderContext, surfaceSize.X, surfaceSize.Y);
            canvas = surface?.Canvas;
            CallerInfo = CallerInfo.InRenderer(surfaceSize.X, surfaceSize.Y, canvas, renderContext.SkiaContext, scaling);
        }

        if (surface is null)
            return;

        if (scaling != lastScaling)
        {
            lastScaling = scaling;
            CallerInfo = CallerInfo.InRenderer(surfaceSize.X, surfaceSize.Y, canvas, renderContext.SkiaContext, scaling);
        }

        if (!lastSetVSync.HasValue || vsync != lastSetVSync.Value)
        {
            lastSetVSync = vsync;
            eglContext.SwapInterval(vsync ? 1 : 0);
        }

        using (new SKAutoCanvasRestore(canvas, true))
        {
            renderAction(CallerInfo);
        }
        surface.Flush();

        eglContext.SwapBuffers(eglSurface);
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        DestroySurface();
    }

    private void DestroySurface()
    {
        DestroyResource(ref surface);
        DestroyResource(ref eglSurface);
    }

    private void DestroyResource<T>(ref T? resource) where T : class, IDisposable
    {
        try
        {
            Interlocked.Exchange(ref resource, null)?.Dispose();
        }
        catch (Exception e)
        {
            var logger = AppHost.CurrentOrGlobal?.DefaultLogger;
            if (logger != null)
                logger.Log(LogLevel.Error, e, "Failed to destroy resource");
        }
    }

    private static SKSurface CreateSkSurface(RenderContext renderContext, int width, int height)
    {
        var sampleCount = 0;
        var stencilBits = 0;
        var colorType = SKColorType.Rgba8888;

        var glInfo = new GRGlFramebufferInfo(
            fboId: 0,
            format: colorType.ToGlSizedFormat());

        using var renderTarget = new GRBackendRenderTarget(
            width: width,
            height: height,
            sampleCount: sampleCount,
            stencilBits: stencilBits,
            glInfo: glInfo);

        return SKSurface.Create(renderContext.SkiaContext, renderTarget, GRSurfaceOrigin.BottomLeft, colorType);
    }
}
