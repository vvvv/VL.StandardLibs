using System;
using System.Diagnostics;

namespace VL.Skia
{
    public class RenderStopwatch
    {
        /// <summary>
        /// The time between StartRender and EndRender in μs.
        /// </summary>
        public float RenderTime { get; private set; }

        Stopwatch stopwatch = Stopwatch.StartNew();
        private TimeSpan tStart;

        public void StartRender()
        {
            tStart = stopwatch.Elapsed;
        }

        public void EndRender()
        {
            RenderTime = (float)((stopwatch.Elapsed - tStart).TotalMilliseconds * 1000);
        }

        public void Reset()
        {
            RenderTime = 0;
        }
    }
}