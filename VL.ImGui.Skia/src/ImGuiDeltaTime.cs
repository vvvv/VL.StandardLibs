using System.Diagnostics;

namespace VL.ImGui
{
    /// <summary>
    /// The time ImGui's clock advances with each frame (<c>io.DeltaTime</c>): the real time since the frame before, not the fixed 1/60 s ImGui assumes when nothing sets it. A layer is updated
    /// when its patch runs, which is not every 1/60 s (a host which runs it for each exchange with a window, or at another rate): with the fixed step the blink of a caret, the delay of a
    /// tooltip and every other ImGui time ran faster the more often the layer was updated. Kept within what ImGui can use: it asserts on a delta of zero or less, and a pause (a breakpoint,
    /// a hidden window) must not make everything which waits on the clock fire at once.
    /// </summary>
    internal sealed class ImGuiDeltaTime
    {
        public const float Default = 1f / 60f;
        public const float Min = 1f / 1000f;
        public const float Max = 1f / 10f;

        long previous;

        /// <param name="timestamp"><see cref="Stopwatch.GetTimestamp"/> of this frame.</param>
        public float Step(long timestamp)
        {
            var delta = previous == 0 ? Default : (float)((timestamp - previous) / (double)Stopwatch.Frequency);
            previous = timestamp;
            return Math.Clamp(delta, Min, Max);
        }
    }
}
