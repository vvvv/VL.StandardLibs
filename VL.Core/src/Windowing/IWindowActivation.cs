#nullable enable

namespace VL.Core.Windowing
{
    /// <summary>
    /// Optional feature of a window (<see cref="IAppWindow.GetFeature{T}"/>): a window that is never activated, neither when it is shown nor when it is clicked,
    /// and so never takes the keyboard focus from the window the user works in. For overlays, together with <see cref="IAppWindow.IsAlwaysOnTop"/>.
    /// Windows offers it (<c>WS_EX_NOACTIVATE</c>); windows without it return null (GLFW has no such attribute; X11 could, with <c>WM_HINTS</c> input = False,
    /// not done yet). <see cref="AppWindowOptions.ShowWithoutActivation"/> is the part every system has: showing without taking the focus.
    /// </summary>
    public interface IWindowActivation
    {
        /// <summary>The window is never activated. A window that never activates does not show in the task bar of Windows either.</summary>
        bool NeverActivates { get; set; }
    }
}
