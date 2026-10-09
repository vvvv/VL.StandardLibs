#nullable enable

using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Forms;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;
using VL.Skia.Egl;
using Keys = System.Windows.Forms.Keys;
using Vector2 = Stride.Core.Mathematics.Vector2;

namespace VL.Skia
{
    public partial class SkiaControl : Control, IProjectionSpace, IWorldSpace2d
    {
        private SkiaInputDevices? inputDevices;
        private readonly Subject<TouchNotification> touchNotifications = new();
        private ISkiaRenderer? renderer;

        public SkiaControl()
        {
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            ResizeRedraw = true;

            var lostfocus = Observable.Never<EventPattern<EventArgs>>()
                .Merge(Observable.FromEventPattern<EventHandler, EventArgs>(addHandler: h => MouseLeave += h, removeHandler: h => MouseLeave -= h)) // leave with mouse
                .Merge(Observable.FromEventPattern<EventHandler, EventArgs>(addHandler: h => LostFocus += h, removeHandler: h => LostFocus -= h)) // alt-tab away from window
                .Select(p => p.EventArgs.ToLostFocusNotification(this, this));
            var gotfocus = Observable.Never<EventPattern<EventArgs>>()
                .Merge(Observable.FromEventPattern<EventHandler, EventArgs>(addHandler: h => MouseEnter += h, removeHandler: h => MouseEnter -= h)) // enter with mouse
                .Merge(Observable.FromEventPattern<EventHandler, EventArgs>(addHandler: h => GotFocus += h, removeHandler: h => GotFocus -= h)) // alt-tab into window
                .Select(p => p.EventArgs.ToGotFocusNotification(this, this));
            Notifications = Observable.Merge(new IObservable<INotification>[] {
                Mouse.Notifications,
                Keyboard.Notifications,
                TouchDevice.Notifications,
                lostfocus,
                gotfocus
                });
        }

        public CallerInfo CallerInfo { get; protected set; } =  CallerInfo.Default;
        public event Action<CallerInfo>? OnRender;
        public float RenderTime => renderer?.RenderTime ?? 0;
        public bool VSync { get; set; }
        public RenderContextProvider? RenderContextProvider { get; init; }
        public bool DirectCompositionEnabled { get; init; }
        public bool TreatAllKeysAsInputKeys { get; init; }

        public Mouse Mouse => (inputDevices ??= new SkiaInputDevices(this, touchNotifications)).Mouse;
        public Keyboard Keyboard => (inputDevices ??= new SkiaInputDevices(this, touchNotifications)).Keyboard;
        public TouchDevice TouchDevice => (inputDevices ??= new SkiaInputDevices(this, touchNotifications)).TouchDevice;
        public IObservable<TouchNotification> TouchNotifications => touchNotifications;
        public IObservable<INotification> Notifications { get; }

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
            base.OnHandleCreated(e);
            if (RenderContextProvider != null)
            {
                renderer = new EglSkiaRenderer(RenderContextProvider);
                DoubleBuffered = false;
            }
            else
            {
                renderer = new SoftwareSkiaRenderer();
                DoubleBuffered = true;
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);
            renderer?.Dispose();
            renderer = null;
        }

        protected override sealed void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!Visible || Handle == 0)
                return;
            DoRender(e);
        }

        void DoRender(PaintEventArgs e)
        {
            var r = renderer;
            if (r is null)
                return;

            try
            {
                r.Render(Handle, Width, Height, this.LogicalToDeviceScalingFactor(), VSync, ci =>
                {
                    CallerInfo = ci;
                    OnPaint(ci);
                }, e.Graphics);
            }
            catch (Exception ex) when (ex is EglException)
            {
                Invalidate();
            }
        }

        protected virtual void OnPaint(CallerInfo callerInfo) => OnRender?.Invoke(callerInfo);

        public void MapFromPixels(INotificationWithPosition notification, out Vector2 inNormalizedProjection, out Vector2 inProjection)
            => SpaceHelpers.DoMapFromPixels(notification.Position, notification.ClientArea, out inNormalizedProjection, out inProjection);

        public Vector2 MapFromPixels(INotificationWithPosition notification) => notification.Position;

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            if (TreatAllKeysAsInputKeys)
                e.IsInputKey = true;
        }

        protected override void WndProc(ref Message m)
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(8) && TouchMessageProcessor.TryHandle(ref m, this, touchNotifications))
                return;
            base.WndProc(ref m);
        }
    }
}