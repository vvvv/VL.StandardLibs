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
        var mouseDowns = Observable.FromEventPattern<MouseEventArgs>(c, nameof(c.MouseDown))
            .Select(p => p.EventArgs.ToMouseDownNotification(c, c));
        var mouseMoves = Observable.FromEventPattern<MouseEventArgs>(c, nameof(c.MouseMove))
            .Select(p => p.EventArgs.ToMouseMoveNotification(c, c));
        var mouseUps = Observable.FromEventPattern<MouseEventArgs>(c, nameof(c.MouseUp))
            .Select(p => p.EventArgs.ToMouseUpNotification(c, c));
        var mouseWheels = Observable.FromEventPattern<MouseEventArgs>(c, nameof(c.MouseWheel))
            .Select(p => p.EventArgs.ToMouseWheelNotification(c, c));
        return new Mouse(mouseDowns.Merge<MouseNotification>(mouseMoves).Merge(mouseUps).Merge(mouseWheels));
    }

    static Keyboard CreateKeyboard(Control c)
    {
        var keyDowns = Observable.FromEventPattern<KeyEventArgs>(c, nameof(c.KeyDown))
            .Select(p => p.EventArgs.ToKeyDownNotification(c));
        var keyUps = Observable.FromEventPattern<KeyEventArgs>(c, nameof(c.KeyUp))
            .Select(p => p.EventArgs.ToKeyUpNotification(c));
        var keyPresses = Observable.FromEventPattern<KeyPressEventArgs>(c, nameof(c.KeyPress))
            .Select(p => p.EventArgs.ToKeyPressNotification(c));
        return new Keyboard(keyDowns.Merge<KeyNotification>(keyUps).Merge(keyPresses));
    }
}
