#nullable enable
using Stride.Core.Mathematics;
using System;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;

namespace VL.Core.Windowing
{
    /// <summary>
    /// A top level native window. All members must be used on the UI thread of the app (the thread that runs the main loop and created the window);
    /// a call from any other thread throws an <see cref="InvalidOperationException"/>. Nothing is marshaled.
    /// </summary>
    public interface IAppWindow : IDisposable
    {
        /// <summary>Unique within the process and stable for the life of the window. Notifications carry the window as <see cref="INotification.Sender"/>.</summary>
        nint Id { get; }

        bool IsDisposed { get; }

        string Title { get; set; }

        bool Visible { get; set; }

        /// <summary>Has the keyboard focus. The mouse over the window is not focus.</summary>
        bool IsFocused { get; }

        /// <summary>Position of the client area in virtual screen pixels.</summary>
        Int2 Position { get; set; }

        /// <summary>Size of the client area in pixels.</summary>
        Int2 ClientSize { get; set; }

        /// <summary>
        /// The frame around the client area (borders, title bar), in pixels: the outer bounds of the window are the client bounds
        /// (<see cref="Position"/>, <see cref="ClientSize"/>) grown by it. Zero where the window has no frame (borderless, fullscreen)
        /// and where the window system does not say (X11 without a window manager).
        /// </summary>
        WindowFrameMargins FrameMargins { get; }

        /// <summary>Pixels per DIP (1 at 96 dpi, 1.5 at 144 dpi). A change is announced by <see cref="WindowDpiChangedNotification"/>.</summary>
        float ScaleFactor { get; }

        WindowState State { get; set; }

        /// <summary>Borderless on the display the window is on. Switching it off restores the bounds and the decorations the window had before.</summary>
        bool IsFullscreen { get; set; }

        bool IsBorderless { get; set; }

        bool IsResizable { get; set; }

        /// <summary>Above the other windows. A change, by the program or by the user (the system menu of a Windows window), is announced by <see cref="WindowAlwaysOnTopChangedNotification"/>.</summary>
        bool IsAlwaysOnTop { get; set; }

        WindowCursor Cursor { get; set; }

        /// <summary>Raises the window to the front and tries to give it the focus.</summary>
        void Activate();

        /// <summary>Changes position and client size (both in pixels) in one go.</summary>
        void SetBounds(Int2 position, Int2 clientSize);

        /// <summary>
        /// Everything the user or the system does to the window, in the order it happened: mouse, keyboard, touch, text input, drops and the
        /// window notifications (<see cref="WindowResizedNotification"/>, <see cref="WindowMovedNotification"/>, ...).
        /// <see cref="GotFocusNotification"/> and <see cref="LostFocusNotification"/> mean the keyboard focus and nothing else (a <see cref="KeyboardLostNotification"/>
        /// comes before the <see cref="LostFocusNotification"/>); the mouse entering and leaving the client area is <see cref="MouseEnterNotification"/> and
        /// <see cref="MouseLostNotification"/>.
        /// Hot and shared; raised on the UI thread, in the call that pumps the events. Positions are in client pixels.
        /// A subscriber sets <see cref="INotification.Handled"/> to tell the window that it dealt with the notification:
        /// for <see cref="WindowCloseRequestedNotification"/> that means "do not close".
        /// </summary>
        IObservable<INotification> Notifications { get; }

        IMouse Mouse { get; }

        IKeyboard Keyboard { get; }

        ITouchDevice TouchDevice { get; }

        /// <summary>The handle of the OS window for interop (screenshots, native rendering APIs).</summary>
        NativeWindowHandle NativeHandle { get; }

        /// <summary>
        /// Optional features of a window, like <see cref="IGlWindowSurface"/> or <see cref="IWindowTitleBar"/>. Returns null if the window does not have it. Never throws.
        /// </summary>
        T? GetFeature<T>() where T : class;
    }

    public enum WindowState
    {
        Normal,
        Minimized,
        Maximized
    }

    public enum WindowCursor
    {
        Default,
        Hidden,
        Arrow,
        IBeam,
        Crosshair,
        Hand,
        ResizeHorizontal,
        ResizeVertical,
        ResizeAll,
        NotAllowed,
    }

    public enum NativeWindowKind
    {
        None,
        Win32,
        X11,
        Wayland,
        Cocoa
    }

    /// <summary>
    /// The handle(s) of an OS window. Win32: <see cref="Handle"/> = HWND.
    /// X11: <see cref="Handle"/> = Window, <see cref="Display"/> = Display*.
    /// Wayland: <see cref="Handle"/> = wl_surface*, <see cref="Display"/> = wl_display*.
    /// Cocoa: <see cref="Handle"/> = NSWindow*, <see cref="View"/> = NSView*.
    /// </summary>
    public readonly record struct NativeWindowHandle(NativeWindowKind Kind, nint Handle, nint Display = 0, nint View = 0);

    /// <summary>The sizes of the frame of a window around its client area (<see cref="IAppWindow.FrameMargins"/>), in pixels.</summary>
    public readonly record struct WindowFrameMargins(int Left, int Top, int Right, int Bottom);
}
