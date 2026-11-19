#nullable enable
using Microsoft.Extensions.Logging;
using SkiaSharp;
using Stride.Core.Mathematics;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using VL.Core;
using VL.Core.Windowing;
using VL.Skia.Egl;

namespace VL.Skia;

/// <summary>
/// Skia through ANGLE (OpenGL ES on Direct3D 11) into a Win32 window. The GPU context is the one of the <see cref="RenderContextProvider"/>,
/// so the window shares it with everything else that draws with Skia (and Stride, whose device the provider uses) on that thread.
/// Needs nothing but the window handle: the size of the client area and the scale are read from the window on every <see cref="Render"/>.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class AngleWindowSurface : ISkiaWindowSurface
{
    private readonly nint hwnd;
    private readonly RenderContextProvider renderContextProvider;
    private readonly IPresentedFrames? presentedFrames;
    private readonly RenderStopwatch stopwatch = new();
    private RenderContext? lastRenderContext;
    private EglSurface? eglSurface;
    private SKSurface? surface;
    private Int2 surfaceSize;
    private SKCanvas? canvas;
    private bool? lastSetVSync;
    private bool disposed;
    private float lastScaling;

    /// <param name="hwnd">The window to draw into. The surface does not own it.</param>
    /// <param name="renderContextProvider">Provides the ANGLE context, usually <see cref="AppHostExtensions.GetRenderContextProvider(AppHost)"/> of the app.</param>
    /// <param name="presentedFrames">The window's count of presented frames, told of each frame this surface presents. Null for a window that does not count (a control of the editor).</param>
    public AngleWindowSurface(nint hwnd, RenderContextProvider renderContextProvider, IPresentedFrames? presentedFrames = null)
    {
        this.presentedFrames = presentedFrames;
        if (hwnd == 0)
            throw new ArgumentException("No window handle", nameof(hwnd));
        this.hwnd = hwnd;
        this.renderContextProvider = renderContextProvider ?? throw new ArgumentNullException(nameof(renderContextProvider));
    }

    public bool VSync { get; set; } = true;

    public float RenderTime => stopwatch.RenderTime;

    public CallerInfo CallerInfo { get; private set; } = CallerInfo.Default;

    /// <summary>
    /// Draws into the client area of the window and presents. Throws an <see cref="EglException"/> when the surface could not be made or presented;
    /// the next call starts with a new one.
    /// </summary>
    public bool Render(Action<CallerInfo> renderAction)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!GetClientRect(hwnd, out var clientRect))
            return false;
        var width = clientRect.Right - clientRect.Left;
        var height = clientRect.Bottom - clientRect.Top;
        if (width <= 0 || height <= 0)
            return false;
        var dpi = GetDpiForWindow(hwnd);
        var scaling = dpi > 0 ? dpi / 96f : 1f;

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
            return PaintCore(renderContext, width, height, scaling, renderAction);
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

    private bool PaintCore(RenderContext renderContext, int width, int height, float scaling, Action<CallerInfo> renderAction)
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
            return false;

        using var _ = renderContext.MakeCurrent(forRendering: true, eglSurface);

        if (surface is null)
        {
            surface = CreateSkSurface(renderContext, surfaceSize.X, surfaceSize.Y);
            canvas = surface?.Canvas;
            CallerInfo = CallerInfo.InRenderer(surfaceSize.X, surfaceSize.Y, surface, renderContext.SkiaContext, scaling);
        }

        if (surface is null)
            return false;

        if (scaling != lastScaling)
        {
            lastScaling = scaling;
            CallerInfo = CallerInfo.InRenderer(surfaceSize.X, surfaceSize.Y, surface, renderContext.SkiaContext, scaling);
        }

        if (!lastSetVSync.HasValue || VSync != lastSetVSync.Value)
        {
            lastSetVSync = VSync;
            eglContext.SwapInterval(VSync ? 1 : 0);
        }

        using (new SKAutoCanvasRestore(canvas, true))
        {
            renderAction(CallerInfo);
        }
        surface.Flush();

        eglContext.SwapBuffers(eglSurface);
        presentedFrames?.FramePresented();
        return true;
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

    private static void DestroyResource<T>(ref T? resource) where T : class, IDisposable
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

        return SKSurface.Create(renderContext.SkiaContext, renderTarget, GRSurfaceOrigin.BottomLeft, colorType, colorspace: SKColorSpace.CreateSrgb());
    }

    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetClientRect(nint hWnd, out RECT lpRect);

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(nint hwnd);
}
