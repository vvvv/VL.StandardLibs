#nullable enable
using Stride.Core.Mathematics;

namespace VL.Core.Windowing
{
    /// <summary>
    /// Feature of a window created with <see cref="WindowGraphicsApi.OpenGL"/> (<see cref="IAppWindow.GetFeature{T}"/>):
    /// an OpenGL context bound to the window. Everything on the UI thread.
    /// </summary>
    public interface IGlWindowSurface
    {
        /// <summary>Makes the context of the window current on the calling thread.</summary>
        void MakeCurrent();

        void SwapBuffers();

        /// <summary>0: do not wait for the display, 1: wait for every refresh.</summary>
        void SetSwapInterval(int interval);

        /// <summary>The function pointer of an OpenGL function of the current context, or 0.</summary>
        nint GetProcAddress(string name);

        /// <summary>True for an OpenGL ES context, false for desktop OpenGL.</summary>
        bool IsOpenGlEs { get; }

        /// <summary>Size of the drawable in pixels. This is what the framebuffer 0 has.</summary>
        Int2 DrawableSize { get; }
    }
}
