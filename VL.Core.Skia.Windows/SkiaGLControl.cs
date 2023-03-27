using System;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;
using System.Windows.Forms;
using SkiaSharp;
using Vector2 = Stride.Core.Mathematics.Vector2;
using VL.Skia.Egl;

namespace VL.Skia
{
    public partial class SkiaGLControl : UserControl, IProjectionSpace, IWorldSpace2d
    {
        private readonly RenderStopwatch renderStopwatch = new RenderStopwatch();
        private RenderContext renderContext;
        private EglSurface eglSurface;
        private SKSurface surface;
        private SKSizeI surfaceSize;
        private SKCanvas canvas;
        private bool? lastSetVSync;

        public CallerInfo CallerInfo { get; private set; } =  CallerInfo.Default;

        internal EglContext EglContext => renderContext?.EglContext;

        internal GRContext SkiaContext => renderContext?.SkiaContext;

        public event Action<CallerInfo> OnRender;

        /// <summary>
        /// The time to evaluate the layer input in μs.
        /// </summary>
        public float RenderTime => renderStopwatch.RenderTime;

        public bool VSync { get; set; }

        public bool DirectCompositionEnabled { get; init; }

        public SkiaGLControl()
        {
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            DoubleBuffered = false;
            ResizeRedraw = true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                if (DirectCompositionEnabled)
                    createParams.ExStyle |= (int)Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP;
                return createParams;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            DestroySurface();

            renderContext?.Dispose();

            // Retrieve the render context. Doing so in the constructor can lead to crashes when running unit tests with headless machines.
            renderContext = RenderContext.ForCurrentThread();

            eglSurface = EglContext?.CreatePlatformWindowSurface(Handle, DirectCompositionEnabled);
            lastSetVSync = default;

            base.OnHandleCreated(e);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);

            DestroySurface();

            renderContext?.Dispose();
            renderContext = null;
        }

        protected override void OnResize(EventArgs e)
        {
            if (EglContext != null)
            {
                if (!DirectCompositionEnabled)
                {
                    eglSurface?.Dispose();
                    eglSurface = EglContext.CreatePlatformWindowSurface(Handle, DirectCompositionEnabled);
                }
                lastSetVSync = default;
            }

            base.OnResize(e);
        }

        protected override sealed void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!Visible || eglSurface is null)
                return;

            renderStopwatch.StartRender();
            try
            {
                // Ensure our GL context is current on current thread
                EglContext.MakeCurrent(eglSurface);

                // Set VSync
                if (!lastSetVSync.HasValue || VSync != lastSetVSync.Value)
                {
                    lastSetVSync = VSync;
                    EglContext.SwapInterval(VSync ? 1 : 0);
                }

                // Create offscreen surface to render into
                var size = new SKSizeI(Width, Height);
                if (surface is null || size != surfaceSize)
                {
                    surfaceSize = size;
                    surface?.Dispose();
                    surface = CreateSkSurface(renderContext, size.Width, size.Height);
                    canvas = surface.Canvas;
                    CallerInfo = CallerInfo.InRenderer(size.Width, size.Height, canvas, renderContext.SkiaContext);
                }

                // Render
                using (new SKAutoCanvasRestore(canvas, true))
                {
                    OnPaint(CallerInfo);
                }
                surface.Flush();

                // Swap 
                EglContext.SwapBuffers(eglSurface);
            }
            finally
            {
                renderStopwatch.EndRender();
            }
        }

        protected virtual void OnPaint(CallerInfo callerInfo)
        {
            OnRender?.Invoke(CallerInfo);
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

        private void DestroySurface()
        {
            surface?.Dispose();
            surface = null;

            eglSurface?.Dispose();
            eglSurface = default;
        }

        public void MapFromPixels(INotificationWithPosition notification, out Vector2 inNormalizedProjection, out Vector2 inProjection)
        {
            SpaceHelpers.DoMapFromPixels(notification.Position, notification.ClientArea, out inNormalizedProjection, out inProjection);
        }

        public Vector2 MapFromPixels(INotificationWithPosition notification)
        {
            return notification.Position;
        }

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            e.IsInputKey = true;
        }
    }
}