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
        /// Enables fixed increment time mode.
        /// </summary>
        bool IsIncremental { get; set; }

        /// <summary>
        /// Gets or sets the time increment. Used when <see cref="IsIncremental"/> is set to true.
        /// </summary>
        TimeSpan Increment { get; set; }

        /// <summary>
        /// The time interval between the last frame and the current frame.
        /// </summary>
        TimeSpan LastInterval { get; }

        /// <summary>
        /// Occurs when the Timer has completed one interval.
        /// </summary>
        event EventHandler<TimeSpan> Tick;

        void Start();

        void Step();

        void Stop();

        TimerMode Mode { get; set; }
    }

    public static class MainloopTimerExtensions
    {
        public static TimeSpan GetIntervalOrIncrement(this IMainLoopTimer timer)
            => timer.IsIncremental ? timer.Increment : timer.LastInterval;
    }
}
