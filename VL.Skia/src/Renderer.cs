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

namespace VL.Skia
{
    public partial class SkiaRenderer : Form, IDisposable
    {
        SkiaGLControl FControl;
        bool HasValidLayer;
        ILayer Layer;
        AppHost FAppHost;

        public ILayer Input
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

        public void SetSize(System.Drawing.Rectangle boundsInDIP) //obsolete
        {
            var boundsinPix = DIPHelpers.DIPToPixel(boundsInDIP);
            var visible = Screen.AllScreens.Any(s => s.Bounds.IntersectsWith(boundsinPix));

            if (!visible)
            {
                boundsInDIP = GetCenteredBoundsInDIP(boundsInDIP.Width, boundsInDIP.Height);
                boundsinPix = DIPHelpers.DIPToPixel(boundsInDIP);
            }

            if (boundsinPix != Bounds)
                Bounds = boundsinPix;
        }

        public void SetBounds(RectangleF bounds, bool inDIP, bool setClientSize)
        {
            if (inDIP)
                bounds = DIPHelpers.DIPToPixel(bounds);

            var boundsinPix = Conversions.ToRectangle(ref bounds);
            var visible = Screen.AllScreens.Any(s => s.Bounds.IntersectsWith(boundsinPix));

            if (!visible)
                boundsinPix = GetCenteredBoundsInPixel(boundsinPix.Width, boundsinPix.Height);

            if (setClientSize)
            {
                var location = boundsinPix.Location;
                var size = boundsinPix.Size;
                if (Location != location)
                    Location = location;
                if (ClientSize != size)
                    ClientSize = size;
            }
            else
            {
                if (boundsinPix != Bounds)
                    Bounds = boundsinPix;
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
            Screen bestScreen = null;
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

        public SkiaRenderer(Action<SkiaRenderer> layout)
        {
            FAppHost = AppHost.Current;

            this.SuspendLayout();

            Icon = Properties.Resources.QuadIcon;
            StartPosition = FormStartPosition.Manual;

            FControl = new SkiaGLControl() { DirectCompositionEnabled = false /* Rendering works but GPU is still at 20%, so keep it disabled for now */ };
            FControl.Dock = DockStyle.Fill;
            FControl.OnRender += FControl_OnRender;
            Controls.Add(FControl);

            BoundsChanged = new BehaviorSubject<System.Drawing.Rectangle>(new System.Drawing.Rectangle());
            FBoundsStream = new BehaviorSubject<RectangleF>(new RectangleF());

            TouchDevice = new TouchDevice(FControl.TouchNotifications);
            
            Observable.Merge(new IObservable<INotification>[] {
                Mouse.Notifications,
                Keyboard.Notifications,
                TouchDevice.Notifications,
                BoundsStream.Select(r => new NotificationWithClientArea(r.Size.ToVector2(), ModifierKeys.ToOurs(), this))
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
            var area = Screen.PrimaryScreen.WorkingArea;
            var centerX = (area.Right + area.Left) / 2;
            var centerY = (area.Top + area.Bottom) / 2;
            var center = new Point(centerX, centerY);
            return new System.Drawing.Rectangle(center.X - width / 2, center.Y - height / 2, width, height);
        }

        private static System.Drawing.Rectangle GetCenteredBoundsInDIP(int widthInDIP = 600, int heightInDIP = 400) //obsolete
        {
            var area = Screen.PrimaryScreen.WorkingArea;
            var centerX = (area.Right + area.Left) / 2;
            var centerY = (area.Top + area.Bottom) / 2;
            var centerInDIP = DIPHelpers.DIP(new System.Drawing.Point(centerX, centerY));
            return new System.Drawing.Rectangle(centerInDIP.X - widthInDIP / 2, centerInDIP.Y - heightInDIP / 2, widthInDIP, heightInDIP);
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
            using var _ = FAppHost?.MakeCurrentIfNone();

            try
            {
                if (Visible && HasValidLayer && Input != null)
                {
                    n.Handled = Input.Notify(n, FControl.CallerInfo);
                }
            }
            catch (Exception exception)
            {
                RuntimeGraph.ReportException(exception);
            }
        }

        public new void Update() 
        {
            if (Visible)
            {
                FControl.Update();
                FControl.Invalidate();
            }
        }

        private Mouse FMouse;
        public Mouse Mouse
        {
            get
            {
                if (FMouse == null)
                {
                    var mouseDowns = Observable.FromEventPattern<MouseEventArgs>(this.FControl, "MouseDown")
                        .Select(p => p.EventArgs.ToMouseDownNotification(this.FControl, FControl));
                    var mouseMoves = Observable.FromEventPattern<MouseEventArgs>(this.FControl, "MouseMove")
                        .Select(p => p.EventArgs.ToMouseMoveNotification(this.FControl, FControl));
                    var mouseUps = Observable.FromEventPattern<MouseEventArgs>(this.FControl, "MouseUp")
                        .Select(p => p.EventArgs.ToMouseUpNotification(this.FControl, FControl));
                    var mouseWheels = Observable.FromEventPattern<MouseEventArgs>(this.FControl, "MouseWheel")
                        .Select(p => p.EventArgs.ToMouseWheelNotification(this.FControl, FControl));
                    FMouse = new Mouse(mouseDowns
                        .Merge<MouseNotification>(mouseMoves)
                        .Merge(mouseUps)
                        .Merge(mouseWheels));
                }
                return FMouse;
            }
        }

        private Keyboard FKeyboard;
        public Keyboard Keyboard
        {
            get
            {
                if (FKeyboard == null)
                {
                    var keyDowns = Observable.FromEventPattern<KeyEventArgs>(this.FControl, "KeyDown")
                        .Select(p => p.EventArgs.ToKeyDownNotification(this.FControl));
                    var keyUps = Observable.FromEventPattern<KeyEventArgs>(this.FControl, "KeyUp")
                        .Select(p => p.EventArgs.ToKeyUpNotification(this.FControl));
                    var keyPresses = Observable.FromEventPattern<KeyPressEventArgs>(this.FControl, "KeyPress")
                        .Select(p => p.EventArgs.ToKeyPressNotification(this.FControl));
                    FKeyboard = new Keyboard(keyDowns
                        .Merge<KeyNotification>(keyUps)
                        .Merge(keyPresses));
                }
                return FKeyboard;
            }
        }

        public TouchDevice TouchDevice { get; }

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
