#nullable enable
using Stride.Core.Mathematics;

namespace VL.Core.Windowing
{
    /// <summary>
    /// How a window is created. Use <c>AppWindowOptions.Default with { ... }</c>.
    /// </summary>
    public sealed record AppWindowOptions
    {
        public static AppWindowOptions Default { get; } = new();

        public string Title { get; init; } = "vvvv";

        /// <summary>Client bounds in DIP. Null: <see cref="DefaultSize"/>, centered on the primary display.</summary>
        public RectangleF? Bounds { get; init; }

        /// <summary>Client size in DIP when <see cref="Bounds"/> is null.</summary>
        public Int2 DefaultSize { get; init; } = new(600, 400);

        public bool Visible { get; init; } = true;

        public bool IsBorderless { get; init; }

        public bool IsResizable { get; init; } = true;

        public bool IsAlwaysOnTop { get; init; }

        public bool IsFullscreen { get; init; }

        /// <summary>Do not take the focus when the window is shown.</summary>
        public bool ShowWithoutActivation { get; init; }

        /// <summary>
        /// What the window is going to be drawn with. Some systems need to know at creation time
        /// (an OpenGL window has to be created as one).
        /// </summary>
        public WindowGraphicsApi Graphics { get; init; } = WindowGraphicsApi.None;

        /// <summary>The object that creates the window, for diagnostics.</summary>
        public object? Owner { get; init; }
    }

    public enum WindowGraphicsApi
    {
        /// <summary>The window system does not draw to the window, the caller attaches its own swap chain to <see cref="IAppWindow.NativeHandle"/>.</summary>
        None,
        /// <summary>The window offers <see cref="IGlWindowSurface"/>.</summary>
        OpenGL,
    }
}
