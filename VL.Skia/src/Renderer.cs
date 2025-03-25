#nullable enable

using System;
using System.ComponentModel;
using System.Data;
using Stride.Core.Mathematics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VL.Core;
using VL.UI.Core;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;
using VL.Model;
using VL.Lang.PublicAPI;
using VL.Core.Commands;
using System.Reactive.Disposables;
using VL.Core.Utils;
using System.Reactive;

namespace VL.Skia
{
    public partial class SkiaRenderer : Form, IDisposable
    {
        private readonly AppHost FAppHost;
        private readonly SkiaGLControl FControl;
        private readonly SerialDisposable FDarkModeSubscription = new();

        bool HasValidLayer;
        ILayer? Layer;

        public ILayer? Input
        {
            get { return Layer; }
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

        /// <summary>
        /// The time to evaluate and draw the layer input in μs.
        /// </summary>
        public float RenderTime => FControl.RenderTime;

        [Obsolete]
        public void SetSize(System.Drawing.Rectangle boundsInDIP) //obsolete
        {
            SetBounds(new RectangleF(boundsInDIP.X, boundsInDIP.Y, boundsInDIP.Width, boundsInDIP.Height), inDIP: true);
        }

        public void SetBounds(RectangleF bounds, bool inDIP = true, bool setClientSize = false)
        {
            if (bounds.IsEmpty)
                return;

            var b = Conversions.ToRectangle(ref bounds);
            SetBounds(inDIP ? DIPHelpers.DIPToPixel(b) : b, setClientSize);
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
                            //var b = ComputeFullScreenBounds();
                            //if (b.HasValue)
                            //    Bounds = b.Value;
                            FWindowedBorderStyle = FormBorderStyle;
                            FormBorderStyle = FormBorderStyle.None;

                            // it's important to do this after changing the border style. otherwise it will be too big
                            WindowState = FormWindowState.Maximized;
                        }
                        else
                        {
                            //Bounds = FWindowedBoundsInPix;
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

        private System.Drawing.Rectangle? ComputeFullScreenBounds()
        {
            var maxIntersection = -1;
            Screen? bestScreen = null;
            foreach (Screen screen in Screen.AllScreens)
            {
                var intersection = Bounds;
                intersection.Intersect(screen.Bounds);
                var intersectionSize = intersection.Width * intersection.Height;
                if (intersectionSize > maxIntersection)
                {
                    bestScreen = screen;
                    maxIntersection = intersectionSize;
                }
            }

            if (bestScreen != null)
                return bestScreen.Bounds;
            return null;
        }

        [DllImport("User32.dll")]
        public static extern Int32 SetForegroundWindow(int hWnd);

        public SkiaRenderer()
            : this(null)
        {
            
        }

        public SkiaRenderer(Action<SkiaRenderer>? layout)
        {
            FAppHost = AppHost.Current;

            this.SuspendLayout();

            Icon = Properties.Resources.QuadIcon;
            StartPosition = FormStartPosition.Manual;

            FControl = new SkiaGLControl()
            { 
                DirectCompositionEnabled = false /* Rendering works but GPU is still at 20%, so keep it disabled for now */,
                TreatAllKeysAsInputKeys = true
            };
            FControl.Dock = DockStyle.Fill;
            FControl.OnRender += FControl_OnRender;
            Controls.Add(FControl);

            BoundsChanged = new BehaviorSubject<System.Drawing.Rectangle>(new System.Drawing.Rectangle());
            FBoundsStream = new BehaviorSubject<RectangleF>(new RectangleF());
            var lostfocus = Observable.Never<EventPattern<EventArgs>>()
                .Merge(Observable.FromEventPattern<EventArgs>(FControl, nameof(MouseLeave))) // leave with mouse
                .Merge(Observable.FromEventPattern<EventArgs>(FControl, nameof(LostFocus))) // alt-tab away from window
                //.Merge(Observable.FromEventPattern<EventArgs>(this, nameof(Leave)))
                //.Merge(Observable.FromEventPattern<EventArgs>(this, nameof(DragLeave)))
                .Select(p => p.EventArgs.ToLostFocusNotification(this, this));
            var gotfocus = Observable.Never<EventPattern<EventArgs>>()
                .Merge(Observable.FromEventPattern<EventArgs>(FControl, nameof(MouseEnter))) // enter with mouse
                .Merge(Observable.FromEventPattern<EventArgs>(FControl, nameof(GotFocus))) // alt-tab into window
                .Select(p => p.EventArgs.ToGotFocusNotification(this, this));
            Observable.Merge(new IObservable<INotification>[] {
                Mouse.Notifications,
                Keyboard.Notifications,
                TouchDevice.Notifications,
                BoundsStream.Select(r => new NotificationWithClientArea(r.Size.ToVector2(), ModifierKeys.ToOurs(), this)),
                lostfocus,
                gotfocus
                })
            .Subscribe(OnNotification);

            var size = DIPHelpers.DIPToPixel(new System.Drawing.Size(600, 400));
            var bounds = GetCenteredBoundsInPixel(size.Width, size.Height);
            var boundsF = Conversions.ToRectangleF(ref bounds);
            SetBounds(boundsF, inDIP: false, setClientSize: true);

            layout?.Invoke(this);

            this.ResumeLayout();
        }

        private static System.Drawing.Rectangle GetCenteredBoundsInPixel(int width = 600, int height = 400)
        {
            var area = Screen.PrimaryScreen?.WorkingArea ?? System.Drawing.Rectangle.Empty;
            var centerX = (area.Right + area.Left) / 2;
            var centerY = (area.Top + area.Bottom) / 2;
            var center = new Point(centerX, centerY);
            return new System.Drawing.Rectangle(center.X - width / 2, center.Y - height / 2, width, height);
        }

        bool FFirstRenderCall = true;

        protected override void WndProc(ref Message m)
        {
            try
            {
                base.WndProc(ref m);
            }
            catch (Exception e)
            {
                if (IDevSession.Current != null)
                {
                    // this is only the fallback catch for any message. 
                    // for rendering or input notifications we actually want to catch the exceptions differently
                    // and inform the user via pink nodes. Not some tty output...
                    IDevSession.Current.ReportException(e);
                }
                else
                {
                    // when running standalone there is no session, let the hosting environment deal with it
                    throw;
                }
            }
        }

        private void FControl_OnRender(CallerInfo callerInfo)
        {
            using var _ = FAppHost?.MakeCurrentIfNone();

            try
            {
                if (!FFirstRenderCall && Visible && HasValidLayer)
                    Input?.Render(callerInfo);
                FFirstRenderCall = false;
            }
            catch (Exception exception)
            {
                RuntimeGraph.ReportException(exception);
            }
        }

        private void OnNotification(INotification n)
        {
            // Don't propagate notifications while we are disposing (lost focus event for example)
            if (Disposing)
                return;

            using var _ = FAppHost?.MakeCurrentIfNone();

            try
            {
                if (CommandList != null && CommandList.TryExecute(n))
                {
                    n.Handled = true;
                }
                else if (Visible && HasValidLayer && Input != null)
                {
                    n.Handled = Input.Notify(n, FControl.CallerInfo);
                }
            }
            catch (Exception exception)
            {
                RuntimeGraph.ReportException(exception);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            FDarkModeSubscription.Disposable = DarkTitleBarClass.Install(Handle);

            base.OnHandleCreated(e);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            FDarkModeSubscription.Disposable = null;

            base.OnHandleDestroyed(e);
        }

        public new void Update() 
        {
            if (Visible)
            {
                FControl.Update();
                FControl.Invalidate();
            }
        }

        public Mouse Mouse => FControl.Mouse;

        public Keyboard Keyboard => FControl.Keyboard;

        public TouchDevice TouchDevice => FControl.TouchDevice;

        public ICommandList? CommandList { get; set; }

        public bool VSync
        {
            get => FControl.VSync;
            set => FControl.VSync = value;
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

        protected void OnBoundsChanged()
        {
            BoundsChanged?.OnNext(DIPHelpers.DIP(Bounds));
            var bounds = Bounds;
            var boundsF = Conversions.ToRectangleF(ref bounds);
            boundsF = DIPHelpers.DIP(boundsF);
            FBoundsStream?.OnNext(boundsF);
        }
    }

    //taken from: https://stackoverflow.com/a/10727337
    public class SkiaRendererTopMost: SkiaRenderer
    {
        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                //make sure Top Most property on form is set to false
                //otherwise this doesn't work
                int WS_EX_TOPMOST = 0x00000008;
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOPMOST;
                return cp;
            }
        }
    }
}
