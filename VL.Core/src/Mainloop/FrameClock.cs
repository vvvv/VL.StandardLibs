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

        Time FClientFrameTime;
        double FClientTimeDifference;
        Time FFrameTime;
        ulong FCurrentFrame = 0;
        TimeSpan FLastInterval;
        Subject<FrameTimeMessage> FrameStarting;
        Subject<FrameFinishedMessage> FrameFinished;
        Subject<SubFrameMessage> OnSubFrameEvent;
        bool FInitialized;
        private double desiredTimeDifference = 1.0 / 60.0; //60fps
        private double timeIncrement = 1.0 / 60.0; //60fps
        private bool isIncremental = false;
        private double waitAccuracy = 2.0 / 1000.0; //2ms
        IMainLoopTimer mainLoopTimer;
        readonly Stopwatch FWatch = Stopwatch.StartNew();
        readonly Stopwatch FTotalTimeWatch = Stopwatch.StartNew();
        private Time FIncrementalTime = TimeSpan.Zero;
        private bool wasInIncrementalMode;

        public FrameClock()
        {
            FrameStarting = new Subject<FrameTimeMessage>();
            FrameFinished = new Subject<FrameFinishedMessage>();
            OnSubFrameEvent = new();
        }

        public Time Time => FFrameTime;

        /// <summary>
        /// Used on start of the frame, sets the frame time and triggers the before frame tick event.
        /// </summary>
        public void BeginFrame() => BeginFrame(IsIncremental);

        internal void BeginFrame(bool isIncremental)
        {
            if (isIncremental)
            {
                TimeDifference = Math.Max(TimeIncrement, MinTimeDifferenceInSeconds);
                FFrameTime += TimeIncrement;
                FIncrementalTime = FFrameTime;
                wasInIncrementalMode = true;
            }
            else
            {
                if (FInitialized)
                    TimeDifference = FWatch.Elapsed.TotalSeconds;
                else
                    TimeDifference = Math.Max(DesiredTimeDifference, MinTimeDifferenceInSeconds);

                if (wasInIncrementalMode)
                {
                    wasInIncrementalMode = false;
                    FTotalTimeWatch.Restart();
                    FIncrementalTime += TimeIncrement;
                }

                FFrameTime = FIncrementalTime + FTotalTimeWatch.Elapsed.TotalSeconds;
            }

            FWatch.Restart();
            FInitialized = true;

            FCurrentFrame++;
            FLastInterval = TimeSpan.FromSeconds(TimeDifference);
            FrameStarting.OnNext(new FrameTimeMessage(FFrameTime, FCurrentFrame, FLastInterval));

            OnSubFrameEvent.OnNext(new SubFrameMessage(FFrameTime, FCurrentFrame, FLastInterval, SubFrameEvents.SubChannelsGetLocked));
            OnSubFrameEvent.OnNext(new SubFrameMessage(FFrameTime, FCurrentFrame, FLastInterval, SubFrameEvents.EnsureMutatingPropertiesAreReflectedInChannels)); 
            OnSubFrameEvent.OnNext(new SubFrameMessage(FFrameTime, FCurrentFrame, FLastInterval, SubFrameEvents.ModulesWriteGlobalChannels));
            OnSubFrameEvent.OnNext(new SubFrameMessage(FFrameTime, FCurrentFrame, FLastInterval, SubFrameEvents.ApplyClientTime));
            OnSubFrameEvent.OnNext(new SubFrameMessage(FFrameTime, FCurrentFrame, FLastInterval, SubFrameEvents.PlayingTransitions));
            OnSubFrameEvent.OnNext(new SubFrameMessage(FFrameTime, FCurrentFrame, FLastInterval, SubFrameEvents.TrackingGlobalChannels));
            OnSubFrameEvent.OnNext(new SubFrameMessage(FFrameTime, FCurrentFrame, FLastInterval, SubFrameEvents.SubChannelsMutateParentChannels));
        }

        public void ApplyClientFrameTime(Time frameTime)
        {
            if (frameTime != FClientFrameTime)
            {
                FFrameTime = frameTime;
                if (FInitialized)
                    TimeDifference = Math.Max(frameTime.Seconds - FClientFrameTime.Seconds, MinTimeDifferenceInSeconds);
                else
                    TimeDifference = DesiredTimeDifference;

                FClientFrameTime = frameTime;
                FClientTimeDifference = TimeDifference;
            }
            else
            {
                FFrameTime = FClientFrameTime;
                TimeDifference = FClientTimeDifference;
            }
        }

        /// <summary>
        /// Used immediately after the frame work is done, sets the frame duration time and triggers the frame finished event.
        /// </summary>
        public void NotifyFrameFinished()
        {
            OnSubFrameEvent.OnNext(new SubFrameMessage(FFrameTime, FCurrentFrame, FLastInterval, SubFrameEvents.ModulesSendingData));

            UpdateTime = FWatch.Elapsed;
            FrameFinished.OnNext(new FrameFinishedMessage(FFrameTime, UpdateTime, FCurrentFrame, FLastInterval));
        }

        public Time UpdateTime { get; private set; }

        public void Restart()
        {
            FInitialized = false;
            FFrameTime = 0;
            FIncrementalTime = 0;
            FWatch.Restart();
            FTotalTimeWatch.Restart();
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
            set => isIncremental = value;
        }

        /// <summary>
        /// Desired increment time in seconds, used if <see cref="IsIncremental"/> is set to true;
        /// Defalt is 1/60.
        /// </summary>
        public double TimeIncrement
        {
            get => timeIncrement;
            set => timeIncrement = value;
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
        public IObservable<SubFrameMessage> GetSubFrameEvents() => OnSubFrameEvent;
    }
}
