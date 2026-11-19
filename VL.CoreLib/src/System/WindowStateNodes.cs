#nullable enable
using System.ComponentModel;
using VL.Core.Import;
using VL.Core.Windowing;
using VL.Lib.Windowing;

[assembly: ImportType(typeof(WindowStateNode), Category = "System.Windows")]
[assembly: ImportType(typeof(SetWindowStateNode), Category = "System.Windows")]
[assembly: ImportType(typeof(SetWindowModeNode), Category = "System.Windows")]

namespace VL.Lib.Windowing;

/// <summary>
/// Returns the state of a window: its title, frame, control box, state and visibility.
/// </summary>
[ProcessNode(Name = "WindowState")]
public sealed class WindowStateNode
{
    public WindowStateNode() { }

    /// <param name="input">The window.</param>
    /// <param name="caption">The title of the window.</param>
    /// <param name="borderStyle">The frame of the window.</param>
    /// <param name="controlBox">Whether the title bar shows the system menu and the minimize, maximize and close buttons. False for a window without a title bar of its own.</param>
    /// <param name="state">Normal, minimized or maximized.</param>
    /// <param name="visible">Whether the window is shown.</param>
    /// <returns>The window.</returns>
    [return: Pin(Name = "Output")]
    public IAppWindow? Update(
        IAppWindow? input,
        [Pin(Name = "Caption")] out string caption,
        [Pin(Name = "Border Style")] out WindowBorderStyle borderStyle,
        [Pin(Name = "Control Box")] out bool controlBox,
        out WindowState state,
        out bool visible)
    {
        if (input is null || input.IsDisposed)
        {
            caption = string.Empty;
            borderStyle = default;
            controlBox = false;
            state = default;
            visible = false;
            return input;
        }

        caption = input.Title;
        borderStyle = input.BorderStyle;
        controlBox = input.GetFeature<IWindowTitleBar>()?.ControlBox ?? false;
        state = input.State;
        visible = input.Visible;
        return input;
    }
}

/// <summary>
/// Sets the title, frame, control box, state and visibility of a window. Each value is applied when it changed, and to a new window.
/// </summary>
[ProcessNode(Name = "SetWindowState")]
public sealed class SetWindowStateNode
{
    private IAppWindow? window;
    private string? caption;
    private WindowBorderStyle? borderStyle;
    private bool? controlBox;
    private WindowState? state;
    private bool? visible;

    public SetWindowStateNode() { }

    /// <param name="input">The window.</param>
    /// <param name="caption">The title of the window.</param>
    /// <param name="borderStyle">The frame of the window.</param>
    /// <param name="controlBox">Whether the title bar shows the system menu and the minimize, maximize and close buttons. Only windows with a title bar of their own (Windows) can switch it.</param>
    /// <param name="state">Normal, minimized or maximized.</param>
    /// <param name="visible">Whether the window is shown.</param>
    /// <returns>The window.</returns>
    [return: Pin(Name = "Output")]
    public IAppWindow? Update(
        IAppWindow? input,
        [Pin(Name = "Caption"), DefaultValue("Renderer")] string caption,
        [Pin(Name = "Border Style"), DefaultValue(WindowBorderStyle.Sizable)] WindowBorderStyle borderStyle,
        [Pin(Name = "Control Box")] bool controlBox,
        WindowState state,
        [DefaultValue(true)] bool visible)
    {
        if (input is null || input.IsDisposed)
            return input;

        if (!ReferenceEquals(input, window))
        {
            window = input;
            this.caption = null;
            this.borderStyle = null;
            this.controlBox = null;
            this.state = null;
            this.visible = null;
        }

        if (this.caption != caption)
            input.Title = (this.caption = caption) ?? string.Empty;
        if (this.borderStyle != borderStyle)
            input.BorderStyle = (this.borderStyle = borderStyle).Value;
        if (this.controlBox != controlBox)
        {
            this.controlBox = controlBox;
            if (input.GetFeature<IWindowTitleBar>() is { } titleBar)
                titleBar.ControlBox = controlBox;
        }
        if (this.state != state)
            input.State = (this.state = state).Value;
        if (this.visible != visible)
            input.Visible = (this.visible = visible).Value;

        return input;
    }
}

/// <summary>
/// Hides a window, shows it in a window or fullscreen on the display it is on.
/// </summary>
[ProcessNode(Name = "SetWindowMode")]
public sealed class SetWindowModeNode
{
    public SetWindowModeNode() { }

    /// <param name="input">The window.</param>
    /// <param name="hide">Hides the window.</param>
    /// <param name="windowed">Shows the window with its frame.</param>
    /// <param name="fullscreen">Shows the window without frame over the whole display it is on, and activates it.</param>
    /// <returns>The window.</returns>
    [return: Pin(Name = "Output")]
    public IAppWindow? Update(IAppWindow? input, bool hide, bool windowed, bool fullscreen)
    {
        if (input is null || input.IsDisposed)
            return input;

        if (hide)
            input.Visible = false;
        if (windowed)
        {
            input.IsFullscreen = false;
            input.Visible = true;
        }
        if (fullscreen)
        {
            input.IsFullscreen = true;
            input.Visible = true;
            input.Activate();
        }
        return input;
    }
}
