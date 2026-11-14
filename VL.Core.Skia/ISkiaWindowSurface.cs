#nullable enable
using System;

namespace VL.Skia
{
    /// <summary>
    /// Draws Skia content into one window. Created per window, owns the GPU resources that belong to the window.
    /// Everything must be called on the UI thread of the app.
    /// </summary>
    public interface ISkiaWindowSurface : IDisposable
    {
        bool VSync { get; set; }

        /// <summary>The time the render action of the last <see cref="Render"/> took, in microseconds.</summary>
        float RenderTime { get; }

        /// <summary>The caller info of the last <see cref="Render"/>.</summary>
        CallerInfo CallerInfo { get; }

        /// <summary>
        /// Makes the window current, runs <paramref name="renderAction"/> with a canvas that covers the client area (in pixels,
        /// <see cref="CallerInfo.Scaling"/> is the scale factor of the window), flushes and presents.
        /// Returns false if nothing was drawn (zero size, window disposed).
        /// </summary>
        bool Render(Action<CallerInfo> renderAction);
    }
}
