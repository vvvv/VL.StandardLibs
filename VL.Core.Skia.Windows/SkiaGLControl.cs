#nullable enable

using System;
using System.Windows.Forms;
using SkiaSharp;
using VL.Skia.Egl;
using Stride.Core.Mathematics;
using VL.Core;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace VL.Skia
{
    public class SkiaGLControl : SkiaControlBase
    {
        private RenderContext? lastRenderContext;
        private EglSurface? eglSurface;
        private SKSurface? surface;
        private Int2 surfaceSize;
        private SKCanvas? canvas;
        private bool? lastSetVSync;

        public SkiaGLControl()
        {
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            DoubleBuffered = false;
            ResizeRedraw = true;
        }

        public required RenderContextProvider RenderContextProvider { get; init; }

        protected override void OnHandleCreated(EventArgs e)
        {
            lastSetVSync = default;

            base.OnHandleCreated(e);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);

            DestroySurface();
        }

        private void DestroyResource<T>(ref T? resource) where T : class, IDisposable
        {
            try
            {
                Interlocked.Exchange(ref resource, null)?.Dispose();
            }
            catch (Exception e)
            {
                AppHost.CurrentOrGlobal?.DefaultLogger.LogError(e, "Failed to destroy resource");
            }
        }

        private void DestroySurface()
        {
            DestroyResource(ref surface);
            DestroyResource(ref eglSurface);
        }

        protected override sealed void OnPaintCore(PaintEventArgs e)
        {
            var renderContext = RenderContextProvider.GetRenderContext();
            if (renderContext != lastRenderContext)
            {
                DestroySurface();
                lastRenderContext = renderContext;
            }

            try
            {
                PaintCore(renderContext);
            }
            catch (EglException)
            {
                DestroySurface();
                Invalidate();
            }
        }

        private void PaintCore(RenderContext renderContext)
        {
            // Ensure our GL context is current on current thread
            var eglContext = renderContext.EglContext;

            // Create offscreen surface to render into
            if (eglSurface is null || surfaceSize.X != Width || surfaceSize.Y != Height)
            {
                using var _1 = eglContext.MakeCurrent(forRendering: false);

                DestroySurface();

                surfaceSize = new Int2(Width, Height);

                eglSurface = eglContext.CreatePlatformWindowSurface(Handle, Width, Height);
                lastSetVSync = default;
            }

            if (eglSurface is null)
                return;

            using var _ = renderContext.MakeCurrent(forRendering: true, eglSurface);

            if (surface is null)
            {
                surface = CreateSkSurface(renderContext, surfaceSize.X, surfaceSize.Y);
                canvas = surface?.Canvas;
                CallerInfo = CallerInfo.InRenderer(surfaceSize.X, surfaceSize.Y, canvas, renderContext.SkiaContext);
            }

            if (surface is null)
                return;

            // Set VSync
            if (!lastSetVSync.HasValue || VSync != lastSetVSync.Value)
            {
                lastSetVSync = VSync;
                eglContext.SwapInterval(VSync ? 1 : 0);
            }

            // Render
            using (new SKAutoCanvasRestore(canvas, true))
            {
                OnPaint(CallerInfo);
            }
            surface.Flush();

            // Swap 
            eglContext.SwapBuffers(eglSurface);
        }

        static SKSurface CreateSkSurface(RenderContext renderContext, int width, int height)
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
}