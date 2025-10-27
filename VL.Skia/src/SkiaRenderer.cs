#nullable enable

extern alias sw;
using Stride.Core.Mathematics;
using sw::System.Windows.Forms;
using sw::VL.Skia;
using System;
using System.Data;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VL.Core;
using VL.Core.Commands;
using VL.Core.Utils;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;
using VL.UI.Core;
using Win32CustomTitleBar = sw::VL.Core.Windows.Win32CustomTitleBar;

namespace VL.Skia
{
    /// <summary>
    /// The render window used in patches. Exposed to user via <see cref="Graphics.Skia.SkiaRendererNode.Form"/> output.
    /// </summary>
    public partial class SkiaRenderer : Form, IDisposable, IProjectionSpace, IWorldSpace2d
    {
        // TODO: How usses this? Seems misplaced
        [DllImport("User32.dll")]
        public static extern Int32 SetForegroundWindow(int hWnd);

        private readonly AppHost FAppHost;
        private readonly SerialDisposable FDarkModeSubscription = new();
        private readonly Subject<TouchNotification> touchNotifications = new();
        private SkiaInputDevices? inputDevices;
        private ISkiaRenderer? renderer;

        bool HasValidLayer;
        ILayer? Layer;
        bool FFirstRenderCall = true;

        public SkiaRenderer(NodeContext nodeContext, Win32CustomTitleBar.Options options)
        {
            FAppHost = nodeContext.AppHost;

            Icon = Properties.Resources.QuadIcon;
            StartPosition = FormStartPosition.Manual;

            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            ResizeRedraw = true;
            DoubleBuffered = false;

            customTitleBar = Win32CustomTitleBar.Install(this, nodeContext, options with { IsFullscreen = () => FFullScreen });

            BoundsChanged = new BehaviorSubject<System.Drawing.Rectangle>(new System.Drawing.Rectangle());
            FBoundsStream = new BehaviorSubject<RectangleF>(new RectangleF());

            var lostfocus = Observable.Never<EventPattern<EventArgs>>()
                .Merge(Observable.FromEventPattern<EventArgs>(this, nameof(MouseLeave)))
                .Merge(Observable.FromEventPattern<EventArgs>(this, nameof(LostFocus)))
                .Select(p => p.EventArgs.ToLostFocusNotification(this, this));
            var gotfocus = Observable.Never<EventPattern<EventArgs>>()
                .Merge(Observable.FromEventPattern<EventArgs>(this, nameof(MouseEnter)))
                .Merge(Observable.FromEventPattern<EventArgs>(this, nameof(GotFocus)))
                .Select(p => p.EventArgs.ToGotFocusNotification(this, this));

            Observable.Merge(new IObservable<INotification>[] {
                Mouse.Notifications,
                Keyboard.Notifications,
                TouchDevice.Notifications,
                FBoundsStream.Select(r => new NotificationWithClientArea(r.Size.ToVector2(), ModifierKeys.ToOurs(), this)),
                lostfocus,
                gotfocus
            })
            .Subscribe(OnNotification);

            var size = LogicalToDeviceUnits(new System.Drawing.Size(600, 400));
            var bounds = GetCenteredBoundsInPixel(size.Width, size.Height);
            var boundsF = Conversions.ToRectangleF(ref bounds);
            SetBounds(boundsF, inDIP: false, setClientSize: true);
        }

        public ILayer? Input
        {
            get => Layer;
            set
            {
                HasValidLayer = true;
                if (Layer != value)
                {
                    Layer = value;
                    if (Layer != null)
                        OnBoundsChanged();
                }
            }
        }

        BehaviorSubject<RectangleF> FBoundsStream;
        public IObservable<RectangleF> BoundsStream => FBoundsStream;
        public BehaviorSubject<System.Drawing.Rectangle> BoundsChanged { get; } // obsolete

        public float RenderTime => renderer?.RenderTime ?? 0;
        public bool VSync { get; set; } = true;
        public RenderContextProvider RenderContextProvider => FAppHost.GetRenderContextProvider();
        public Mouse Mouse => (inputDevices ??= new SkiaInputDevices(this, touchNotifications)).Mouse;
        public Keyboard Keyboard => (inputDevices ??= new SkiaInputDevices(this, touchNotifications)).Keyboard;
        public TouchDevice TouchDevice => (inputDevices ??= new SkiaInputDevices(this, touchNotifications)).TouchDevice;
        public ICommandList? CommandList { get; set; }

        System.Drawing.Rectangle FWindowedBoundsInPix;
        FormBorderStyle FWindowedBorderStyle;
        bool FFullScreen;
        public bool FullScreen
        {
            get => FFullScreen;
            set
            {
                if (FFullScreen != value)
                {
                    SuspendLayout();
                    try
                    {
                        FFullScreen = value;
                        if (value)
                        {
                            FWindowedBoundsInPix = Bounds;
                            FWindowedBorderStyle = FormBorderStyle;
                            FormBorderStyle = FormBorderStyle.None;
                            WindowState = FormWindowState.Maximized;
                        }
                        else
                        {
                            FormBorderStyle = FWindowedBorderStyle;
                            WindowState = FormWindowState.Normal;
                        }
                    }
                    finally
                    {
                        ResumeLayout();
                    }
                }
            }
        }

        // Called from patch
        public void SetSize(System.Drawing.Rectangle boundsInDIP)
        {
            SetBounds(new RectangleF(boundsInDIP.X, boundsInDIP.Y, boundsInDIP.Width, boundsInDIP.Height), inDIP: true);
        }

        public void SetBounds(RectangleF bounds, bool inDIP = true, bool setClientSize = false)
        {
            if (bounds.IsEmpty)
                return;

            var b = Conversions.ToRectangle(ref bounds);
            SetBounds(inDIP ? this.LogicalToDeviceUnits(b) : b, setClientSize);
        }

        private void SetBounds(System.Drawing.Rectangle bounds, bool setClientSize)
        {
            var visible = Screen.AllScreens.Any(s => s.Bounds.IntersectsWith(bounds));

            if (!visible)
                bounds = GetCenteredBoundsInPixel(bounds.Width, bounds.Height);

            if (setClientSize)
            {
                var location = bounds.Location;
                var size = bounds.Size;
                if (Location != location)
                    Location = location;
                if (ClientSize != size)
                    ClientSize = size;
            }
            else
            {
                if (bounds != Bounds)
                    Bounds = bounds;
            }
        }

        static System.Drawing.Rectangle GetCenteredBoundsInPixel(int width = 600, int height = 400)
        {
            var area = Screen.PrimaryScreen?.WorkingArea ?? System.Drawing.Rectangle.Empty;
            var centerX = (area.Right + area.Left) / 2;
            var centerY = (area.Top + area.Bottom) / 2;
            var center = new System.Drawing.Point(centerX, centerY);
            return new System.Drawing.Rectangle(center.X - width / 2, center.Y - height / 2, width, height);
        }

        public new void Update()
        {
            if (Visible)
            {
                base.Update();
                Invalidate();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            FDarkModeSubscription.Disposable = DarkTitleBarClass.Install(Handle);
            base.OnHandleCreated(e);
            renderer = new EglSkiaRenderer(RenderContextProvider);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            FDarkModeSubscription.Disposable = null;
            base.OnHandleDestroyed(e);
            renderer?.Dispose();
            renderer = null;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            OnBoundsChanged();
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            OnBoundsChanged();
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            OnBoundsChanged();
        }

        private void OnBoundsChanged()
        {
            BoundsChanged?.OnNext(this.DeviceToLogicalUnits(Bounds));
            var bounds = Bounds;
            var boundsF = Conversions.ToRectangleF(ref bounds);
            boundsF = this.DeviceToLogicalUnits(boundsF);
            FBoundsStream?.OnNext(boundsF);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!Visible || Handle == 0 || renderer is null)
                return;

            renderer.Render(Handle, ClientSize.Width, ClientSize.Height, this.LogicalToDeviceScalingFactor(), VSync, callerInfo =>
            {
                using var _ = FAppHost?.MakeCurrentIfNone();
                try
                {
                    if (!FFirstRenderCall && Visible && HasValidLayer)
                        Input?.Render(callerInfo);
                    PaintTitleBarButtons(callerInfo.Canvas);
                    FFirstRenderCall = false;
                }
                catch (Exception exception)
                {
                    RuntimeGraph.ReportException(exception);
                }
            }, e.Graphics);
        }

        private void OnNotification(INotification n)
        {
            if (Disposing)
                return;

            using var _ = FAppHost?.MakeCurrentIfNone();

            try
            {
                if (CommandList != null && CommandList.TryExecute(n))
                {
                    n.Handled = true;
                }
                else if (Visible && HasValidLayer && Input != null && renderer is not null)
                {
                    n.Handled = Input.Notify(n, renderer.CallerInfo);
                }
            }
            catch (Exception exception)
            {
                RuntimeGraph.ReportException(exception);
            }
        }

        public void MapFromPixels(INotificationWithPosition notification, out Vector2 inNormalizedProjection, out Vector2 inProjection)
            => SpaceHelpers.DoMapFromPixels(notification.Position, notification.ClientArea, out inNormalizedProjection, out inProjection);

        public Vector2 MapFromPixels(INotificationWithPosition notification) => notification.Position;
    }

    public class SkiaRendererTopMost : SkiaRenderer
    {
        public SkiaRendererTopMost(NodeContext nodeContext) : base(nodeContext, new (AlwaysOnTop: true, ExtendIntoTitleBar: false))
        {
        }

        protected override bool ShowWithoutActivation => true;
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_TOPMOST = 0x00000008;
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOPMOST;
                return cp;
            }
        }
    }
}
