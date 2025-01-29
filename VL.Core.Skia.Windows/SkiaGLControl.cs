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
using System.Drawing.Imaging;
using System.Drawing;
using Rectangle = System.Drawing.Rectangle;
using Color = System.Drawing.Color;

namespace VL.Skia
{
    public partial class SkiaGLControl : Control, IProjectionSpace, IWorldSpace2d
    {
        private readonly RenderStopwatch renderStopwatch = new RenderStopwatch();

        private Mouse? mouse;
        private Keyboard? keyboard;
        private TouchDevice? touchDevice;

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
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            BackColor = Color.Red;

            //SetStyle(ControlStyles.Opaque, true);
            //SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
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
            lastSetVSync = default;

            base.OnHandleCreated(e);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);

            FreeBitmap();
        }

        protected override sealed void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!Visible || Handle == 0)
                return;


            // get the bitmap
            var info = CreateBitmap();

            if (info.Width == 0 || info.Height == 0)
                return;

            var data = bitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

            // create the surface
            using (var surface = SKSurface.Create(info, data.Scan0, data.Stride))
            {
                var canvas = surface.Canvas;
                CallerInfo = CallerInfo.InRenderer(info.Width, info.Height, canvas, null);
                // start drawing
                using (new SKAutoCanvasRestore(surface.Canvas, true))
                {
                    OnPaint(CallerInfo);
                }
                surface.Canvas.Flush();
            }

            // write the bitmap to the graphics
            bitmap.UnlockBits(data);
            e.Graphics.DrawImage(bitmap, 0, 0);
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

        private Bitmap bitmap;

        private SKImageInfo CreateBitmap()
        {
            var info = new SKImageInfo(Width, Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

            if (bitmap == null || bitmap.Width != info.Width || bitmap.Height != info.Height)
            {
                FreeBitmap();

                if (info.Width != 0 && info.Height != 0)
                    bitmap = new Bitmap(info.Width, info.Height, PixelFormat.Format32bppPArgb);
            }

            return info;
        }

        private void FreeBitmap()
        {
            if (bitmap != null)
            {
                bitmap.Dispose();
                bitmap = null;
            }
        }
    }
}