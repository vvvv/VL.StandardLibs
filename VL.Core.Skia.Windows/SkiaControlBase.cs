#nullable enable

using System;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;
using System.Windows.Forms;
using Vector2 = Stride.Core.Mathematics.Vector2;
using System.Reactive.Linq;
using Keys = System.Windows.Forms.Keys;

namespace VL.Skia
{
    public abstract partial class SkiaControlBase : Control, IProjectionSpace, IWorldSpace2d
    {
        private readonly RenderStopwatch renderStopwatch = new RenderStopwatch();

        private Mouse? mouse;
        private Keyboard? keyboard;
        private TouchDevice? touchDevice;

        public CallerInfo CallerInfo { get; protected set; } =  CallerInfo.Default;

        public event Action<CallerInfo>? OnRender;

        /// <summary>
        /// The time to evaluate the layer input in μs.
        /// </summary>
        public float RenderTime => renderStopwatch.RenderTime;

        public bool VSync { get; set; }

        public bool DirectCompositionEnabled { get; init; }

        public bool TreatAllKeysAsInputKeys { get; init; }

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

        protected override sealed void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!Visible || Handle == 0)
                return;

            renderStopwatch.StartRender();
            try
            {
                OnPaintCore(e);
            }
            finally
            {
                renderStopwatch.EndRender();
            }
        }

        protected abstract void OnPaintCore(PaintEventArgs e);

        protected virtual void OnPaint(CallerInfo callerInfo)
        {
            OnRender?.Invoke(CallerInfo);
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
            if (TreatAllKeysAsInputKeys)
                e.IsInputKey = true;
        }
    }
}