#nullable enable

using System;
using System.Reactive.Linq;
using System.Windows.Forms;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;

namespace VL.Skia;

/// <summary>
/// Provides mouse / keyboard / touch devices for any WinForms Control (including Forms) using the
/// same notification conversions SkiaControl previously implemented.
/// </summary>
internal sealed class SkiaInputDevices
{
    private readonly Control control;

    public Mouse Mouse { get; }
    public Keyboard Keyboard { get; }
    public TouchDevice TouchDevice { get; }

    public SkiaInputDevices(Control control, IObservable<TouchNotification> touchNotifications)
    {
        this.control = control ?? throw new ArgumentNullException(nameof(control));

        Mouse = CreateMouse(control);
        Keyboard = CreateKeyboard(control);
        TouchDevice = new TouchDevice(touchNotifications);
    }

    static Mouse CreateMouse(Control c)
    {
        var mouseDowns = Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(addHandler: h => c.MouseDown += h, removeHandler: h => c.MouseDown -= h)
            .Select(p => p.EventArgs.ToMouseDownNotification(c, c));
        var mouseMoves = Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(addHandler: h => c.MouseMove += h, removeHandler: h => c.MouseMove -= h)
            .Select(p => p.EventArgs.ToMouseMoveNotification(c, c));
        var mouseUps = Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(addHandler: h => c.MouseUp += h, removeHandler: h => c.MouseUp -= h)
            .Select(p => p.EventArgs.ToMouseUpNotification(c, c));
        var mouseWheels = Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(addHandler: h => c.MouseWheel += h, removeHandler: h => c.MouseWheel -= h)
            .Select(p => p.EventArgs.ToMouseWheelNotification(c, c));
        return new Mouse(mouseDowns.Merge<MouseNotification>(mouseMoves).Merge(mouseUps).Merge(mouseWheels));
    }

    static Keyboard CreateKeyboard(Control c)
    {
        var keyDowns = Observable.FromEventPattern<KeyEventHandler, KeyEventArgs>(addHandler: h => c.KeyDown += h, removeHandler: h => c.KeyDown -= h)
            .Select(p => p.EventArgs.ToKeyDownNotification(c));
        var keyUps = Observable.FromEventPattern<KeyEventHandler, KeyEventArgs>(addHandler: h => c.KeyUp += h, removeHandler: h => c.KeyUp -= h)
            .Select(p => p.EventArgs.ToKeyUpNotification(c));
        var keyPresses = Observable.FromEventPattern<KeyPressEventHandler, KeyPressEventArgs>(addHandler: h => c.KeyPress += h, removeHandler: h => c.KeyPress -= h)
            .Select(p => p.EventArgs.ToKeyPressNotification(c));
        return new Keyboard(keyDowns.Merge<KeyNotification>(keyUps).Merge(keyPresses));
    }
}
