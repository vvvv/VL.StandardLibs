#nullable enable
using Stride.Core.Mathematics;

namespace VL.Core.Windowing
{
    /// <summary>
    /// Optional feature of a window (<see cref="IAppWindow.GetFeature{T}"/>): the client area can extend into the title bar, the content then draws
    /// the caption buttons where the window says, while the window keeps moving, resizing and the clicks on the buttons. Windows only for now;
    /// windows without it return null.
    /// </summary>
    public interface IWindowTitleBar
    {
        /// <summary>The client area covers the title bar. Has no effect while the window is fullscreen. A change is announced by <see cref="WindowTitleBarChangedNotification"/>.</summary>
        bool ExtendIntoTitleBar { get; set; }

        /// <summary>Width in DIP, from the left of the title bar, where clicks go to the content instead of moving the window. 0: the whole title bar moves the window.</summary>
        int InteractionWidth { get; set; }

        /// <summary>
        /// The control box: the system menu and the minimize, maximize and close buttons of the title bar. Off: the title bar shows the title only.
        /// A feature of the title bar because only Windows can switch it apart from the whole frame (GLFW cannot; X11 only through hints a window manager may ignore).
        /// </summary>
        bool ControlBox { get; set; }

        /// <summary>True while the content has to draw the caption buttons: the title bar is extended and the mouse is over one of the buttons.</summary>
        bool ShouldDrawButtons { get; }

        /// <summary>The minimize button, in pixels of the client area.</summary>
        TitleBarButton MinimizeButton { get; }

        /// <summary>The maximize (restore) button, in pixels of the client area.</summary>
        TitleBarButton MaximizeButton { get; }

        /// <summary>The close button, in pixels of the client area.</summary>
        TitleBarButton CloseButton { get; }
    }

    /// <summary>A caption button of a title bar: where it is (pixels of the client area) and whether the mouse is over it.</summary>
    public readonly record struct TitleBarButton(Rectangle Bounds, bool IsHovered);
}