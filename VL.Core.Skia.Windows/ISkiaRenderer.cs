#nullable enable

using System;
using System.Drawing;

namespace VL.Skia;

internal interface ISkiaRenderer : IDisposable
{
    /// <summary>The time in microseconds spent inside the renderAction of the last Render call.</summary>
    float RenderTime { get; }
    CallerInfo CallerInfo { get; }
    void Render(nint hwnd, int width, int height, bool vsync, Action<CallerInfo> renderAction, Graphics? gdiTarget = null);
}
