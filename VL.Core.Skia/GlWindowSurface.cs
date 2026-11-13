#nullable enable
using SkiaSharp;
using System;
using Stride.Core.Mathematics;
using VL.Core.Windowing;

namespace VL.Skia
{
    /// <summary>
    /// Skia on the OpenGL context of a window (<see cref="IGlWindowSurface"/>): macOS and Linux, and Windows windows that don't use ANGLE.
    /// </summary>
    public sealed class GlWindowSurface : ISkiaWindowSurface
    {
        /// <summary>
        /// Creates a surface for a window that was created with <see cref="WindowGraphicsApi.OpenGL"/>.
        /// Returns null if the window has no OpenGL context.
        /// </summary>
        public static GlWindowSurface? TryCreate(IAppWindow window)
        {
            var gl = window.GetFeature<IGlWindowSurface>();
            return gl is null ? null : new GlWindowSurface(window, gl);
        }

        readonly IAppWindow window;
        readonly IGlWindowSurface gl;
        readonly RenderStopwatch stopwatch = new();
        GRGlInterface? glInterface;
        GRContext? grContext;
        SKSurface? surface;
        Int2 surfaceSize;
        float lastScaling;
        bool vsync = true;
        bool vsyncDirty = true;
        bool disposed;

        GlWindowSurface(IAppWindow window, IGlWindowSurface gl)
        {
            this.window = window;
            this.gl = gl;
        }

        public bool VSync
        {
            get => vsync;
            set
            {
                if (vsync != value)
                {
                    vsync = value;
                    vsyncDirty = true;
                }
            }
        }

        public float RenderTime => stopwatch.RenderTime;

        public CallerInfo CallerInfo { get; private set; } = CallerInfo.Default;

        public bool Render(Action<CallerInfo> renderAction)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(GlWindowSurface));
            if (window.IsDisposed)
                return false;

            var size = gl.DrawableSize;
            if (size.X <= 0 || size.Y <= 0)
                return false;

            gl.MakeCurrent();
            if (grContext is null && !CreateContext())
                return false;

            var scaling = window.ScaleFactor;
            if (surface is null || size != surfaceSize)
            {
                surface?.Dispose();
                surface = CreateSurface(size);
                surfaceSize = size;
                lastScaling = 0;
            }
            if (surface is null)
                return false;

            if (scaling != lastScaling)
            {
                lastScaling = scaling;
                CallerInfo = CallerInfo.InRenderer(size.X, size.Y, surface, grContext!, scaling);
            }

            if (vsyncDirty)
            {
                vsyncDirty = false;
                gl.SetSwapInterval(vsync ? 1 : 0);
            }

            stopwatch.StartRender();
            try
            {
                var canvas = surface.Canvas;
                using (new SKAutoCanvasRestore(canvas, true))
                    renderAction(CallerInfo);
                surface.Flush();
            }
            finally
            {
                stopwatch.EndRender();
            }

            gl.SwapBuffers();
            return true;
        }

        bool CreateContext()
        {
            // Skia's own loader first: it knows which functions it needs and how the platform finds them.
            // The function pointers of the window library are the fallback.
            glInterface = GRGlInterface.Create() ?? GRGlInterface.Create(name => gl.GetProcAddress(name));
            if (glInterface is null)
                return false;
            grContext = GRContext.CreateGl(glInterface);
            return grContext is not null;
        }

        SKSurface? CreateSurface(Int2 size)
        {
            var colorType = SKColorType.Rgba8888;
            using var renderTarget = new GRBackendRenderTarget(
                width: size.X,
                height: size.Y,
                sampleCount: 0,
                stencilBits: 8,
                glInfo: new GRGlFramebufferInfo(fboId: 0, format: colorType.ToGlSizedFormat()));
            return SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.BottomLeft, colorType, colorspace: SKColorSpace.CreateSrgb());
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            if (window.IsDisposed)
            {
                // The GL context is gone with the window: nothing to release
                grContext?.AbandonContext(releaseResources: false);
            }
            else
            {
                gl.MakeCurrent();
            }
            surface?.Dispose();
            grContext?.Dispose();
            glInterface?.Dispose();
            surface = null;
            grContext = null;
            glInterface = null;
        }
    }
}
