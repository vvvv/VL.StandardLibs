using System;
using System.Reactive.Disposables;
using VL.Core;

namespace VL.Lib.Animation
{
    public struct FrameTimeMessage
    {
        /// <summary>
        /// The frame time since application start
        /// </summary>
        public readonly Time Time;

        public readonly ulong FrameNumber;

        /// <summary>
        /// The time interval between the last frame and the current frame.
        /// </summary>
        public readonly TimeSpan LastInterval;

        public FrameTimeMessage(Time time, ulong frameNumber, TimeSpan lastInterval)
        {
            Time = time;
            FrameNumber = frameNumber;
            LastInterval = lastInterval;
        }
    }

    public struct FrameFinishedMessage
    {
        /// <summary>
        /// The frame time since application start
        /// </summary>
        public readonly Time Time;

        /// <summary>
        /// The elapsed time since the current frame has started
        /// </summary>
        public readonly Time FrameDuration;

        public readonly ulong FrameNumber;

        /// <summary>
        /// The time interval between the last frame and the current frame.
        /// </summary>
        public readonly TimeSpan LastInterval;

        public FrameFinishedMessage(Time time, Time frameDuration, ulong frameNumber, TimeSpan lastInterval)
        {
            Time = time;
            FrameDuration = frameDuration;
            FrameNumber = frameNumber;
            LastInterval = lastInterval;
        }
    }

    public interface IClock
    {
        Time Time { get; }
    }

    public interface IFrameClock : IClock
    {
        /// <summary>
        /// Gets the time difference between the frame time of the last frame to the current frame time.
        /// </summary>
        double TimeDifference { get; }

        /// <summary>
        /// Gets an observable that sends an event before each frame.
        /// </summary>
        IObservable<FrameTimeMessage> GetTicks();

        /// <summary>
        /// Gets an observable that sends an event directly after the Update call of each frame.
        /// </summary>
        IObservable<FrameFinishedMessage> GetFrameFinished();
    }

    public static class Clocks
    {
        [ThreadStatic]
        private static IFrameClock currentFrameClock;

        /// <summary>
        /// Sets the clock for the current thread. The returned disposable will set the previous clock when disposed.
        /// </summary>
        public static IDisposable SetCurrentFrameClock(IFrameClock clock)
        {
            var previousClock = currentFrameClock;
            currentFrameClock = clock;
            return Disposable.Create(() =>
            {
                currentFrameClock = previousClock;
            });
        }

        /// <summary>
        /// The clock used to determine the time when the current frame of the vl mainloop started
        /// </summary>
        public static IFrameClock FrameClock => currentFrameClock ??= AppHost.Current.Services.GetService<IFrameClock>();

        public static IClock DefaultClock => FrameClock;

        /// <summary>
        /// The time when the current frame started
        /// </summary>
        public static Time CurrentFrameTime => FrameClock.Time;

        /// <summary>
        /// The clock used to determine the current time, returns a new time value for each call
        /// </summary>
        public static IClock RealTimeClock => AppHost.Current.Services.GetService<IClock>();

        /// <summary>
        /// Checks time now
        /// </summary>
        public static Time CurrentTime => RealTimeClock.Time;

        public static double Seconds(this IClock clock) => clock.Time.Seconds;

    }
}
