#nullable enable

using System;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;
using System.Windows.Forms;
using Vector2 = Stride.Core.Mathematics.Vector2;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Keys = System.Windows.Forms.Keys;
using VL.Skia.Egl;

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
                r.Render(Handle, Width, Height, VSync, ci =>
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

        protected virtual void OnPaint(CallerInfo callerInfo) => OnRender?.Invoke(CallerInfo);

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