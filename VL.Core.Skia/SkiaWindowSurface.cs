#nullable enable
using System;
using VL.Core;
using VL.Core.Windowing;

namespace VL.Skia;

/// <summary>
/// Skia in a window of the window contract: which graphics API a window needs to be drawn with Skia on this system, and the surface that draws into it.
/// </summary>
public static class SkiaWindowSurface
{
    /// <summary>
    /// What a window has to be created with (<see cref="AppWindowOptions.Graphics"/>) to be drawn with Skia here.
    /// Windows: <see cref="WindowGraphicsApi.None"/>, Skia draws with ANGLE into the HWND, on the Direct3D device it shares with Stride.
    /// Elsewhere: <see cref="WindowGraphicsApi.OpenGL"/>, Skia draws on the OpenGL context of the window.
    /// </summary>
    public static WindowGraphicsApi GraphicsApi => OperatingSystem.IsWindows() ? WindowGraphicsApi.None : WindowGraphicsApi.OpenGL;

    /// <summary>
    /// The surface for a window created with <see cref="GraphicsApi"/>: <see cref="GlWindowSurface"/> for a window with an OpenGL context,
    /// <see cref="AngleWindowSurface"/> (on the render context of <paramref name="appHost"/>) for a Win32 window.
    /// </summary>
    public static ISkiaWindowSurface Create(IAppWindow window, AppHost appHost)
    {
        if (GlWindowSurface.TryCreate(window) is { } gl)
            return gl;
        if (OperatingSystem.IsWindows() && window.NativeHandle.Kind == NativeWindowKind.Win32)
            return new AngleWindowSurface(window.NativeHandle.Handle, appHost.GetRenderContextProvider());
        throw new NotSupportedException($"Skia cannot draw into this window ({window.NativeHandle.Kind}, no OpenGL context): create it with {nameof(AppWindowOptions.Graphics)} = {nameof(SkiaWindowSurface)}.{nameof(GraphicsApi)}.");
    }
}