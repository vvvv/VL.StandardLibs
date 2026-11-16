#nullable enable

using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows.Forms;
using Stride.Core.Mathematics;
using VL.Core.Utils;
using VL.Core.Windowing;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using Rectangle = System.Drawing.Rectangle;
using AppWindowState = VL.Core.Windowing.WindowState;

namespace VL.Core.Windows;

/// <summary>
/// A Windows Forms window as an <see cref="IAppWindow"/>: what the render window of patches (the <c>Renderer</c> of VL.Skia) is on Windows.
/// Created by <see cref="WinFormsAppWindowService"/>. All members of the contract throw on another thread than the one that created the window.
/// </summary>
/// <remarks>
/// Fullscreen is borderless and covers the bounds of the display the window is on; the state (normal, maximized) stays what it was and comes back with the bounds.
/// The window has the custom title bar of <see cref="Win32CustomTitleBar"/>: its system menu ("always on top", "extend into title bar") and the feature <see cref="IWindowTitleBar"/>.
/// Touch comes from the pointer messages (Windows 8 and later). Focus notifications mean the keyboard focus; the mouse entering and leaving is
/// <see cref="MouseEnterNotification"/> and <see cref="MouseLostNotification"/>. The window offers <see cref="IWindowActivation"/> (<c>WS_EX_NOACTIVATE</c>) and counts the frames
/// presented into it (<see cref="IPresentedFrames"/>).
/// </remarks>
public sealed class WinFormsAppWindow : Form, IAppWindow, IWindowActivation, IPresentedFrames
{
    static int lastId;

    private readonly WinFormsAppWindowService service;
    private readonly Thread thread;
    private readonly AppWindowOptions options;
    private readonly Subject<INotification> notifications = new();
    private readonly IObserver<TouchNotification> touchSink;
    private readonly Win32CustomTitleBar titleBar;
    private readonly SerialDisposable darkTitleBar = new();
    private Mouse? mouse;
    private VL.Lib.IO.Keyboard? keyboard;
    private TouchDevice? touchDevice;

    private WindowBorderStyle borderStyle;
    private bool isFullscreen;
    private FormBorderStyle windowedBorderStyle;
    private Rectangle windowedBounds;
    private FormWindowState windowedState;
    private WindowCursor cursor;
    private bool cursorHiddenByUs;
    private bool neverActivates;
    private Int2 lastClientSize;
    private Int2 lastPosition;
    private AppWindowState lastState;
    private bool closedRaised;

    private static readonly Lazy<System.Drawing.Icon> VvvvIcon = new(() => new System.Drawing.Icon(typeof(WinFormsAppWindow).Assembly.GetManifestResourceStream("VL.Core.Windows.vvvv.ico")!));

    internal WinFormsAppWindow(WinFormsAppWindowService service, AppWindowOptions options)
    {
        if (options.Graphics != WindowGraphicsApi.None)
            throw new NotSupportedException($"A Windows Forms window has no {options.Graphics} context: create it with {nameof(WindowGraphicsApi)}.{nameof(WindowGraphicsApi.None)} and draw into its native handle (Skia: ANGLE, see SkiaWindowSurface of VL.Core.Skia).");

        this.service = service;
        this.options = options;
        thread = Thread.CurrentThread;
        Id = Interlocked.Increment(ref lastId);
        touchSink = Observer.Create<TouchNotification>(Raise);

        // The surface of the window draws everything: no background, no double buffer of Windows Forms
        SetStyle(ControlStyles.Opaque, true);
        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        DoubleBuffered = false;
        StartPosition = FormStartPosition.Manual;
        Text = options.Title;
        Icon = VvvvIcon.Value;

        titleBar = new Win32CustomTitleBar(this) { IsFullScreen = () => isFullscreen, AlwaysOnTop = options.IsAlwaysOnTop };
        titleBar.PropertyChanged += OnTitleBarChanged;

        borderStyle = options.BorderStyle;
        FormBorderStyle = WindowedBorderStyle;

        // The handle exists from the start: the native handle of the contract does not change, DPI and the client area are known
        CreateHandle();

        var bounds = options.Bounds is { } b
            ? this.LogicalToDeviceUnits(new Rectangle((int)b.X, (int)b.Y, (int)b.Width, (int)b.Height))
            : CenteredOnPrimary(this.LogicalToDeviceUnits(new Size(options.DefaultSize.X, options.DefaultSize.Y)));
        if (!Screen.AllScreens.Any(s => s.Bounds.IntersectsWith(bounds)))
            bounds = CenteredOnPrimary(bounds.Size);
        ClientSize = bounds.Size;
        MoveClientTo(new Int2(bounds.X, bounds.Y));

        lastClientSize = ToInt2(ClientSize);
        lastPosition = ClientPosition;
        lastState = ToOurs(WindowState);
    }

    // Contract

    public nint Id { get; }

    bool IAppWindow.IsDisposed => IsDisposed;

    string IAppWindow.Title
    {
        get { Check(); return Text; }
        set { Check(); Text = value; }
    }

    bool IAppWindow.Visible
    {
        get { Check(); return Visible; }
        set { Check(); Visible = value; }
    }

    bool IAppWindow.IsFocused
    {
        get { Check(); return ContainsFocus; }
    }

    Int2 IAppWindow.Position
    {
        get { Check(); return ClientPosition; }
        set { Check(); MoveClientTo(value); }
    }

    Int2 IAppWindow.ClientSize
    {
        get { Check(); return ToInt2(ClientSize); }
        set { Check(); ClientSize = new Size(value.X, value.Y); }
    }

    WindowFrameMargins IAppWindow.FrameMargins
    {
        get
        {
            Check();
            // The outer bounds are what GetWindowRect says (with the invisible resize borders of Windows 10 and later): what the render window of patches stores
            var outer = Bounds;
            var origin = PointToScreen(Point.Empty);
            var client = ClientSize;
            return new(origin.X - outer.Left, origin.Y - outer.Top, outer.Right - origin.X - client.Width, outer.Bottom - origin.Y - client.Height);
        }
    }

    float IAppWindow.ScaleFactor
    {
        get { Check(); return this.LogicalToDeviceScalingFactor(); }
    }

    AppWindowState IAppWindow.State
    {
        get { Check(); return ToOurs(isFullscreen ? windowedState : WindowState); }
        set
        {
            Check();
            var state = value switch { AppWindowState.Minimized => FormWindowState.Minimized, AppWindowState.Maximized => FormWindowState.Maximized, _ => FormWindowState.Normal };
            if (isFullscreen)
                windowedState = state;
            else
                WindowState = state;
        }
    }

    bool IAppWindow.IsFullscreen
    {
        get { Check(); return isFullscreen; }
        set { Check(); SetFullscreen(value); }
    }

    WindowBorderStyle IAppWindow.BorderStyle
    {
        get { Check(); return borderStyle; }
        set { Check(); borderStyle = value; ApplyBorderStyle(); }
    }

    bool IAppWindow.IsAlwaysOnTop
    {
        get { Check(); return titleBar.AlwaysOnTop; }
        set { Check(); titleBar.AlwaysOnTop = value; }
    }

    WindowCursor IAppWindow.Cursor
    {
        get { Check(); return cursor; }
        set { Check(); SetCursor(value); }
    }

    void IAppWindow.Activate()
    {
        Check();
        Activate();
    }

    void IAppWindow.SetBounds(Int2 position, Int2 clientSize)
    {
        Check();
        ClientSize = new Size(clientSize.X, clientSize.Y);
        MoveClientTo(position);
    }

    public IObservable<INotification> Notifications => notifications;

    public IMouse Mouse => mouse ??= new Mouse(notifications.OfType<MouseNotification>());

    public IKeyboard Keyboard => keyboard ??= new VL.Lib.IO.Keyboard(notifications.OfType<KeyNotification>());

    public ITouchDevice TouchDevice => touchDevice ??= new TouchDevice(notifications.OfType<TouchNotification>());

    NativeWindowHandle IAppWindow.NativeHandle
    {
        get { Check(); return new NativeWindowHandle(NativeWindowKind.Win32, Handle); }
    }

    T? IAppWindow.GetFeature<T>() where T : class
    {
        if (typeof(T) == typeof(IWindowTitleBar))
            return (T)(object)titleBar;
        if (typeof(T) == typeof(IWindowActivation) || typeof(T) == typeof(IPresentedFrames))
            return (T)(object)this;
        return null;
    }

    private long presentedFrames;

    long IPresentedFrames.PresentedFrames => presentedFrames;

    void IPresentedFrames.FramePresented() => presentedFrames++;

    bool IWindowActivation.NeverActivates
    {
        get { Check(); return neverActivates; }
        set
        {
            Check();
            if (neverActivates == value)
                return;
            neverActivates = value;
            // Applies the extended style of CreateParams to the window that exists
            if (IsHandleCreated)
                UpdateStyles();
        }
    }

    // Windows Forms

    protected override bool ShowWithoutActivation => options.ShowWithoutActivation || neverActivates;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_NOACTIVATE = 0x08000000;
            var cp = base.CreateParams;
            if (neverActivates)
                cp.ExStyle |= WS_EX_NOACTIVATE;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        darkTitleBar.Disposable = DarkTitleBarClass.Install(Handle);
        base.OnHandleCreated(e);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        darkTitleBar.Disposable = null;
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(8) && TouchMessageProcessor.TryHandle(ref m, this, touchSink))
            return;
        if (titleBar.ProcessMessage(ref m))
            return;
        base.WndProc(ref m);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Raise(e.ToMouseMoveNotification(this, this));
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Raise(e.ToMouseDownNotification(this, this));
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        Raise(e.ToMouseUpNotification(this, this));
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        Raise(e.ToMouseWheelNotification(this, this));
    }

    // The mouse entering and leaving is not focus: own notifications
    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (cursor == WindowCursor.Hidden)
            HideSystemCursor();
        Raise(new MouseEnterNotification(PointToClient(MousePosition).ToVector2(), ClientSize.ToVector2(), ModifierKeys.ToOurs(), this));
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        ShowSystemCursor();
        Raise(e.ToMouseLostNotification(this, this));
    }

    // The keyboard focus
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Raise(e.ToGotFocusNotification(this, this));
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Raise(new KeyboardLostNotification(this));
        Raise(e.ToLostFocusNotification(this, this));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        Raise(e.ToKeyDownNotification(this));
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        Raise(e.ToKeyUpNotification(this));
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        Raise(e.ToKeyPressNotification(this));
        // No input method support: the printable characters of the keyboard layout are the text input
        if (!char.IsControl(e.KeyChar))
            Raise(new TextInputNotification(e.KeyChar.ToString(), ModifierKeys.ToOurs(), this));
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        var size = ToInt2(ClientSize);
        if (size != lastClientSize && WindowState != FormWindowState.Minimized)
        {
            lastClientSize = size;
            Raise(new WindowResizedNotification(size, this.LogicalToDeviceScalingFactor(), this));
        }
        RaiseStateChangedIfNeeded();
        RaiseMovedIfNeeded();
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        RaiseMovedIfNeeded();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        Raise(new WindowDpiChangedNotification(e.DeviceDpiOld / 96f, e.DeviceDpiNew / 96f, this));
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (e.Cancel)
            return;
        var request = new WindowCloseRequestedNotification(this);
        Raise(request);
        e.Cancel = request.Handled;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !closedRaised)
        {
            closedRaised = true;
            ShowSystemCursor();
            notifications.OnNext(new WindowClosedNotification(this));
            notifications.OnCompleted();
            service.OnWindowDisposed(this);
            darkTitleBar.Dispose();
        }
        base.Dispose(disposing);
        if (disposing)
            notifications.Dispose();
    }

    // Helpers

    private void OnTitleBarChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Win32CustomTitleBar.AlwaysOnTop))
            Raise(new WindowAlwaysOnTopChangedNotification(titleBar.AlwaysOnTop, this));
        else if (e.PropertyName == nameof(Win32CustomTitleBar.ExtendIntoTitleBar))
            Raise(new WindowTitleBarChangedNotification(titleBar.ExtendIntoTitleBar, this));
    }

    private void Check()
    {
        if (Thread.CurrentThread != thread)
            throw new InvalidOperationException("A window must be used on the thread that created it (the UI thread of the app).");
        ObjectDisposedException.ThrowIf(IsDisposed, this);
    }

    private void Raise(INotification notification)
    {
        if (!closedRaised)
            notifications.OnNext(notification);
    }

    private FormBorderStyle WindowedBorderStyle => borderStyle switch
    {
        WindowBorderStyle.None => FormBorderStyle.None,
        WindowBorderStyle.Fixed => FormBorderStyle.FixedSingle,
        _ => FormBorderStyle.Sizable,
    };

    private void ApplyBorderStyle()
    {
        if (isFullscreen)
            windowedBorderStyle = WindowedBorderStyle;
        else
            FormBorderStyle = WindowedBorderStyle;
    }

    private void SetFullscreen(bool value)
    {
        if (isFullscreen == value)
            return;

        SuspendLayout();
        try
        {
            if (value)
            {
                windowedState = WindowState;
                windowedBorderStyle = FormBorderStyle;
                WindowState = FormWindowState.Normal;
                windowedBounds = Bounds;
                var screen = Screen.FromHandle(Handle);
                isFullscreen = true;
                FormBorderStyle = FormBorderStyle.None;
                Bounds = screen.Bounds;
            }
            else
            {
                isFullscreen = false;
                FormBorderStyle = windowedBorderStyle;
                Bounds = windowedBounds;
                WindowState = windowedState;
            }
        }
        finally
        {
            ResumeLayout();
        }
        Raise(new WindowStateChangedNotification(ToOurs(WindowState), isFullscreen, this));
    }

    private void SetCursor(WindowCursor value)
    {
        cursor = value;
        Cursor = value switch
        {
            WindowCursor.Arrow => Cursors.Arrow,
            WindowCursor.IBeam => Cursors.IBeam,
            WindowCursor.Crosshair => Cursors.Cross,
            WindowCursor.Hand => Cursors.Hand,
            WindowCursor.ResizeHorizontal => Cursors.SizeWE,
            WindowCursor.ResizeVertical => Cursors.SizeNS,
            WindowCursor.ResizeAll => Cursors.SizeAll,
            WindowCursor.NotAllowed => Cursors.No,
            _ => Cursors.Default,
        };
        // Hidden: the system cursor is hidden while it is over the window (as the render window of patches does)
        if (value == WindowCursor.Hidden && ClientRectangle.Contains(PointToClient(System.Windows.Forms.Cursor.Position)))
            HideSystemCursor();
        else if (value != WindowCursor.Hidden)
            ShowSystemCursor();
    }

    private void HideSystemCursor()
    {
        if (!cursorHiddenByUs)
        {
            cursorHiddenByUs = true;
            System.Windows.Forms.Cursor.Hide();
        }
    }

    private void ShowSystemCursor()
    {
        if (cursorHiddenByUs)
        {
            cursorHiddenByUs = false;
            System.Windows.Forms.Cursor.Show();
        }
    }

    private Int2 ClientPosition => ToInt2(PointToScreen(Point.Empty));

    private void MoveClientTo(Int2 position)
    {
        var offset = PointToScreen(Point.Empty);
        Location = new Point(Location.X + position.X - offset.X, Location.Y + position.Y - offset.Y);
    }

    private void RaiseMovedIfNeeded()
    {
        var position = ClientPosition;
        if (position != lastPosition && WindowState != FormWindowState.Minimized)
        {
            lastPosition = position;
            Raise(new WindowMovedNotification(position, this));
        }
    }

    private void RaiseStateChangedIfNeeded()
    {
        var state = ToOurs(WindowState);
        if (state != lastState)
        {
            lastState = state;
            Raise(new WindowStateChangedNotification(state, isFullscreen, this));
        }
    }

    private static Rectangle CenteredOnPrimary(Size size)
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? Rectangle.Empty;
        return new Rectangle((area.Left + area.Right - size.Width) / 2, (area.Top + area.Bottom - size.Height) / 2, size.Width, size.Height);
    }

    private static AppWindowState ToOurs(FormWindowState state) => state switch
    {
        FormWindowState.Minimized => AppWindowState.Minimized,
        FormWindowState.Maximized => AppWindowState.Maximized,
        _ => AppWindowState.Normal,
    };

    private static Int2 ToInt2(Size size) => new(size.Width, size.Height);

    private static Int2 ToInt2(Point point) => new(point.X, point.Y);
}