#nullable enable

using System;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;
using System.Windows.Forms;
using SkiaSharp;
using Vector2 = Stride.Core.Mathematics.Vector2;
using VL.Skia.Egl;
using Stride.Core.Mathematics;
using System.Reactive.Linq;

namespace VL.Skia
{
    public partial class SkiaGLControl : UserControl, IProjectionSpace, IWorldSpace2d
    {
        private readonly RenderStopwatch renderStopwatch = new RenderStopwatch();

        private Mouse? mouse;
        private Keyboard? keyboard;
        private TouchDevice? touchDevice;

        private RenderContext? renderContext;
        private EglSurface? eglSurface;
        private SKSurface? surface;
        private Int2 surfaceSize;
        private SKCanvas? canvas;
        private bool? lastSetVSync;

        public CallerInfo CallerInfo { get; private set; } =  CallerInfo.Default;

        public event Action<CallerInfo>? OnRender;

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

        public Mouse Mouse => mouse ??= CreateMouse();

        public Keyboard Keyboard => keyboard ??= CreateKeyboard();

        public TouchDevice TouchDevice => touchDevice ??= new TouchDevice(TouchNotifications);

        Mouse CreateMouse()
        {
            var mouseDowns = Observable.FromEventPattern<MouseEventArgs>(this, nameof(MouseDown))
                .Select(p => p.EventArgs.ToMouseDownNotification(this, this));
            var mouseMoves = Observable.FromEventPattern<MouseEventArgs>(this, nameof(MouseMove))
                .Select(p => p.EventArgs.ToMouseMoveNotification(this, this));
            var mouseUps = Observable.FromEventPattern<MouseEventArgs>(this, nameof(MouseUp))
                .Select(p => p.EventArgs.ToMouseUpNotification(this, this));
            var mouseWheels = Observable.FromEventPattern<MouseEventArgs>(this, nameof(MouseWheel))
                .Select(p => p.EventArgs.ToMouseWheelNotification(this, this));
            return new Mouse(mouseDowns
                .Merge<MouseNotification>(mouseMoves)
                .Merge(mouseUps)
                .Merge(mouseWheels));
        }

        Keyboard CreateKeyboard()
        {
            var keyDowns = Observable.FromEventPattern<KeyEventArgs>(this, nameof(KeyDown))
                .Select(p => p.EventArgs.ToKeyDownNotification(this));
            var keyUps = Observable.FromEventPattern<KeyEventArgs>(this, nameof(KeyUp))
                .Select(p => p.EventArgs.ToKeyUpNotification(this));
            var keyPresses = Observable.FromEventPattern<KeyPressEventArgs>(this, nameof(KeyPress))
                .Select(p => p.EventArgs.ToKeyPressNotification(this));
            return new Keyboard(keyDowns
                .Merge<KeyNotification>(keyUps)
                .Merge(keyPresses));
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

        protected override sealed void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!Visible || renderContext is null || Handle == 0)
                return;

            renderStopwatch.StartRender();
            try
            {
                // Ensure our GL context is current on current thread
                var eglContext = renderContext.EglContext;

                // Create offscreen surface to render into
                if (eglSurface is null || surfaceSize.X != Width || surfaceSize.Y != Height)
                {
                    DestroySurface();

                    surfaceSize = new Int2(Width, Height);

                    eglSurface = eglContext.CreatePlatformWindowSurface(Handle, Width, Height);
                }

                if (eglSurface is null)
                    return;

                eglContext.MakeCurrent(eglSurface);

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
            var eglContext = renderContext?.EglContext;
            if (eglContext is null)
                return;

            eglContext.MakeCurrent(eglSurface);

            surface?.Dispose();
            surface = null;

            eglSurface?.Dispose();
            eglSurface = default;

            eglContext.MakeCurrent(null);
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