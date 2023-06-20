using System;
using System.Diagnostics;
using System.Reactive.Subjects;
using VL.Core;

namespace VL.Lib.Animation
{
    public class FrameClock : IFrameClock
    {
        /// <summary>
        /// One micro second. Minimal time difference of frame clock, avoids DivideByZero exceptions in animation nodes.
        /// </summary>
        public const double MinTimeDifferenceInSeconds = 0.000001;

        Time FFrameTime;
        ulong FCurrentFrame = 0;
        Subject<FrameTimeMessage> FrameStarting;
        Subject<FrameFinishedMessage> FrameFinished;
        bool FInitialized;
        private double desiredTimeDifference = 1.0 / 60.0; //60fps
        private double timeIncrement = 1.0 / 60.0; //60fps
        private bool isIncremental = false; //60fps
        private double waitAccuracy = 2.0 / 1000.0; //2ms
        IMainLoopTimer mainLoopTimer;
        readonly Stopwatch FWatch = Stopwatch.StartNew();

        public FrameClock()
        {
            FrameStarting = new Subject<FrameTimeMessage>();
            FrameFinished = new Subject<FrameFinishedMessage>();
        }

        public Time Time => FFrameTime;

        /// <summary>
        /// Used on start of the frame, sets the frame time and triggers the before frame tick event.
        /// </summary>
        /// <param name="frameTime">The absolute time since start. Global parameter used by the animation nodes</param>
        public void SetFrameTime(Time frameTime)
        {
            if (FInitialized)
                TimeDifference = Math.Max(frameTime.Seconds - FFrameTime.Seconds, MinTimeDifferenceInSeconds);
            else
                TimeDifference = DesiredTimeDifference;

            FFrameTime = frameTime;
            FInitialized = true;
            FWatch.Restart();

            FCurrentFrame++;
            FrameStarting.OnNext(new FrameTimeMessage(FFrameTime, FCurrentFrame, mainLoopTimer?.GetIntervalOrIncrement() ?? TimeSpan.Zero));
        }

        /// <summary>
        /// Used immediately after the frame work is done, sets the frame duration time and triggers the frame finished event.
        /// </summary>
        public void NotifyFrameFinished()
        {
            UpdateTime = FWatch.Elapsed;
            FrameFinished.OnNext(new FrameFinishedMessage(FFrameTime, UpdateTime, FCurrentFrame, mainLoopTimer?.GetIntervalOrIncrement() ?? TimeSpan.Zero));
        }

        public Time UpdateTime { get; private set; }

        public void Restart()
        {
            FInitialized = false;
            FFrameTime = 0;
            FWatch.Restart();
            TimeDifference = DesiredTimeDifference;
        }

        public double TimeDifference { get; private set; }

        public void SetCurrentMainloopTimer(IMainLoopTimer timer)
        {
            mainLoopTimer = timer;
            SetMainloopTimerProperties();
        }

        /// <summary>
        /// Desired interval time in seconds, also used to initialize the time difference on first frame and restart.
        /// Defalt is 1/60.
        /// </summary>
        public double DesiredTimeDifference
        {
            get => desiredTimeDifference; 
            set
            {
                if (desiredTimeDifference != value)
                {
                    desiredTimeDifference = value;
                    SetMainloopTimerProperties(); 
                }
            }
        }

        /// <summary>
        /// Enables fixed increment time mode.
        /// </summary>
        public bool IsIncremental
        {
            get => isIncremental;
            set
            {
                if (isIncremental != value)
                {
                    isIncremental = value;
                    SetMainloopTimerProperties(); 
                }
            }
        }

        /// <summary>
        /// Desired increment time in seconds, used if <see cref="IsIncremental"/> is set to true;
        /// Defalt is 1/60.
        /// </summary>
        public double TimeIncrement
        {
            get => timeIncrement;
            set
            {
                if (timeIncrement != value)
                {
                    timeIncrement = value;
                    SetMainloopTimerProperties(); 
                }
            }
        }

        /// <summary>
        /// Estimated precision of the accuracy of Sleep(1) on the current machine in seconds.
        /// Default is 0.002s (2ms). Usually between 1ms and 16ms. Higher values make the timer more precise but uses more CPU power.
        /// </summary>
        public double WaitAccuracy
        {
            get => waitAccuracy;
            set
            {
                waitAccuracy = value;
                SetMainloopTimerProperties();
            }
        }

        private void SetMainloopTimerProperties()
        {
            if (mainLoopTimer != null)
            {
                mainLoopTimer.Interval = TimeSpanUtils.FromSecondsPrecise(desiredTimeDifference);
                mainLoopTimer.WaitAccuracy = TimeSpanUtils.FromSecondsPrecise(waitAccuracy);
                mainLoopTimer.IsIncremental = isIncremental;
                mainLoopTimer.Increment = TimeSpanUtils.FromSecondsPrecise(timeIncrement);
            }
        }

        /// <summary>
        /// The elapsed time since start as TimeSpan, setting the property will call SetFrameTime and update the clock variables.
        /// </summary>
        public TimeSpan Elapsed
        {
            get => TimeSpanUtils.FromSecondsPrecise(FFrameTime.Seconds);
        }

        public IObservable<FrameTimeMessage> GetTicks() => FrameStarting;
        public IObservable<FrameFinishedMessage> GetFrameFinished() => FrameFinished;
    }
}
