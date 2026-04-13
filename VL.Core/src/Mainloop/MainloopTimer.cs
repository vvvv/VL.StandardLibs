using System;

namespace VL.Core
{
    public interface IMainLoopTimer : IDisposable
    {
        /// <summary>
        /// Gets or sets the desired interval. This is the minimum time that has to pass between two frame ticks.
        /// Default is 1/60 of a second.
        /// </summary>
        TimeSpan Interval { get; set; }

        /// <summary>
        /// Estimated precision of the accuracy of Sleep(1) on the current machine.
        /// Default is 2ms. Usually between 1ms and 16ms. Higher values make the timer more precise but uses more CPU power.
        /// </summary>
        TimeSpan WaitAccuracy { get; set; }

        /// <summary>
        /// Occurs when the Timer has completed one interval. Raised on the thread the timer has been created in.
        /// </summary>
        event EventHandler Tick;

        void Start();

        void EmulateTick();

        void Stop();
    }
}
