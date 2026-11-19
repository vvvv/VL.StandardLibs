#nullable enable
namespace VL.Core.Windowing
{
    /// <summary>
    /// Optional feature of a window (<see cref="IAppWindow.GetFeature{T}"/>): the number of frames presented into it since it was made, for tools which watch a program
    /// from outside, such as the runner of the editor, which reports it as the frames of a window of the program. What draws into the window reports each frame it presents
    /// (the Skia surfaces do), so a window nobody draws into, or a paused program, adds nothing.
    /// </summary>
    public interface IPresentedFrames
    {
        /// <summary>The frames presented since the window was made.</summary>
        long PresentedFrames { get; }

        /// <summary>Called by what draws into the window after it presented a frame (swapped the buffers).</summary>
        void FramePresented();
    }
}
