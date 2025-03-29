using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Animation;
using VL.Lib.Reactive;

[assembly: ImportType(typeof(TimerClock), Category = "Reactive.Advanced.Experimental")]
[assembly: ImportType(typeof(BusyWaitTimerObservable), Name = "BusyWaitTimer", Category = "Reactive.Advanced.Experimental")]

namespace VL.Lib.Reactive
{
    public class TimerClock : IFrameClock
    {
        public Time Time
        {
            get;
            internal set;
        }

        public double TimeDifference
        {
            get;
            internal set;
        }

        public long FrameCounter
        {
            get;
            internal set;
        }

        public int FrameCounterInt32 => (int)FrameCounter;

        public Time DurationOfLatestUpdate { get; }

        public IObservable<FrameTimeMessage> GetTicks()
        {
            throw new NotImplementedException();
        }
        public IObservable<FrameFinishedMessage> GetFrameFinished()
        {
            throw new NotImplementedException();
        }
        public IObservable<SubFrameMessage> GetSubFrameEvents()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Timer using the busy wait method, extremely accurate but uses 100% of one core. But hey... don't you have enough of them?
    /// </summary>
    [ProcessNode(FragmentSelection = FragmentSelection.Explicit)]
    public class BusyWaitTimerObservable
    {
        readonly Subject<TimerClock> FTimerSubject;
        readonly SingleAssignmentDisposable FDisposable = new SingleAssignmentDisposable();
        readonly IObservable<TimerClock> FResult;
        readonly TimerClock FFrameClock = new TimerClock();

        /// <summary>
        /// Updates the timer properties.
        /// </summary>
        /// <param name="period">The period time in milliseconds. Values smaller than 1 are possible.</param>
        /// <param name="waitAccuracy">Helps to save CPU power for period times higher than this value. It is the start of the busy wait phase before the peroid end in milliseconds. Before that Thread.Sleep(1) is called periodically, the precision of Thread.Spleep(1) depends on the current windows system timer accuracy, usually between 16ms and 4ms.</param>
        /// <param name="mode"></param>
        /// <param name="threadApartmentState">Set to MTA for background threads and STA if the thread is used for UIs</param>
        /// <param name="resetCounter">If set to <c>true</c> the frame counter will be reset.</param>
        /// <param name="enabled">If set to <c>true</c> the timer runs.</param>
        /// <param name="lastPeriod">Last measured period time in milliseconds.</param>
        /// <returns>Observable of a TimerClock that can be used as frame clock input for animation nodes.</returns> 
        [Fragment]
        public IObservable<TimerClock> Update(out float lastPeriod, float period = 100, int waitAccuracy = 4, TimerMode mode = TimerMode.Periodic, ApartmentState threadApartmentState = ApartmentState.MTA, bool resetCounter = false, bool enabled = true)
        {
            this.Mode = mode;
            this.Period = TimeSpanUtils.FromMillisecondsPrecise(period);
            this.FWaitAccuracy = TimeSpan.FromMilliseconds(waitAccuracy);
            var runningChanged = running != enabled;
            var apartmentChanged = ThreadApartmentState != threadApartmentState;
            this.ThreadApartmentState = threadApartmentState;

            FResetCounter |= resetCounter;

            if (runningChanged)
            {
                if (enabled)
                    Start();
                else
                    Stop();
            }
            else if(running && apartmentChanged)
            {
                Stop();
                Start();
            }

            lastPeriod = (float)LastInterval.TotalMilliseconds;
            return FResult;
        }

        #region Timer Members

        #region Delegates

        // Represents the method that is called by Windows when a timer event occurs.
        private delegate void TimeProc(int id, int msg, int user, int param1, int param2);

        // Represents methods that raise events.
        private delegate void EventRaiser(EventArgs e);

        #endregion

        #region Fields

        // Timer mode.
        private volatile TimerMode mode;

        // Called by Windows when a timer periodic event occurs.
        private TimeProc timeProcPeriodic;

        // Called by Windows when a timer one shot event occurs.
        private TimeProc timeProcOneShot;

        // Indicates whether or not the timer is running.
        private volatile bool running = false;

        // Indicates whether or not the timer has been disposed.
        private volatile bool disposed = false;

        // time measurement
        private Stopwatch stopwatch;

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the Timer has started;
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Occurs when the Timer has stopped;
        /// </summary>
        public event EventHandler Stopped;

        #endregion

        #region Construction


        /// <summary>
        /// Initializes a new instance of the Timer class.
        /// </summary>
        [Fragment]
        public BusyWaitTimerObservable()
        {

            FTimerSubject = new Subject<TimerClock>();
            FDisposable.Disposable = FTimerSubject;
            FResult = FTimerSubject.PubRefCount();

            Initialize();
        }

        // Initialize timer with default values.
        private void Initialize()
        {
            this.mode = TimerMode.Periodic;
            this.Period = TimeSpan.FromMilliseconds(100);

            running = false;

            timeProcOneShot = new TimeProc(TimerOneShotEventCallback);
            timeProcPeriodic = new TimeProc(TimerPeriodicEventCallback);

            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        #endregion

        #region Methods

        TimeSpan FOldTime;
        TimeSpan FInterval;
        TimeSpan FWaitAccuracy = TimeSpan.FromTicks((long)Math.Ceiling(1.5 * TimeSpan.TicksPerMillisecond));

        public TimeSpan LastInterval
        {
            get;
            private set;
        }

        public TimeSpan LastIntervalDiff
        {
            get;
            private set;
        }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// The timer has already been disposed.
        /// </exception>
        /// <exception cref="TimerStartException">
        /// The timer failed to start.
        /// </exception>
        public void Start()
        {
            #region Require

            if (disposed)
            {
                return;
            }

            #endregion

            #region Guard

            if (IsRunning)
            {
                return;
            }

            #endregion

            var thread = new Thread(TimerThreadStart);
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.AboveNormal;
            if (OperatingSystem.IsWindows())
                thread.SetApartmentState(ThreadApartmentState);
            thread.Start();

            // If the timer was created successfully.
            if (thread.IsAlive)
            {
                OnStarted(EventArgs.Empty);
                running = true;
            }
            else
            {
                running = false;
                throw new TimerStartException("Unable to start Timer.");
            }
        }

        private void TimerThreadStart()
        {
            StartedAt = stopwatch.Elapsed;
            try
            {
                // If the periodic event callback should be used.
                if (Mode == TimerMode.Periodic)
                {
                    while (running && Mode == TimerMode.Periodic)
                    {
                        var t = stopwatch.Elapsed;
                        var remainingTime = FInterval - (t - FOldTime);

                        if (remainingTime.Ticks > 2)
                        {
                            //if there is more time left than the wait accuracy
                            if (remainingTime.Ticks > FWaitAccuracy.Ticks)
                            {
                                Thread.Sleep(1);
                                continue;
                            }
                            else //busy wait last time < wait accuracy
                            {
                                while (remainingTime.Ticks > 2)
                                {
                                    //Thread.SpinWait(1)?
                                    t = stopwatch.Elapsed;
                                    remainingTime = FInterval - (t - FOldTime);
                                }
                            }
                        }

                        //if wait timer done, we arrive here
                        var interval = t - FOldTime;
                        LastIntervalDiff = interval - LastInterval;
                        LastInterval = interval;
                        FOldTime = t;

                        //do the actual work
                        if (FResetCounter)
                        {
                            FFramCounter = 0;
                            FResetCounter = false;
                        }
                        FFrameClock.Time = t.TotalSeconds;
                        FFrameClock.TimeDifference = interval.TotalSeconds;
                        FFrameClock.FrameCounter = FFramCounter++;
                        FTimerSubject.OnNext(FFrameClock);
                    }
                }
                else
                {
                    var t = stopwatch.Elapsed;
                    var remainingTime = FInterval - (t - FOldTime);

                    if (remainingTime.Ticks > 0)
                    {
                        //if there is more time left than the wait accuracy
                        while (remainingTime.Ticks > FWaitAccuracy.Ticks)
                        {
                            Thread.Sleep(1);
                        }

                        while (remainingTime.Ticks > 2)
                        {
                            //Thread.SpinWait(1)?
                            t = stopwatch.Elapsed;
                            remainingTime = FInterval - (t - FOldTime);
                        }
                    }

                    //if wait timer done, we arrive here
                    var interval = t - FOldTime;
                    LastIntervalDiff = interval - LastInterval;
                    LastInterval = interval;
                    FOldTime = t;

                    //do the actual work
                    FFrameClock.Time = t.TotalSeconds;
                    FFrameClock.TimeDifference = interval.TotalSeconds;
                    FFrameClock.FrameCounter = FFramCounter++;
                    FTimerSubject.OnNext(FFrameClock);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                //exit thread
            }
        }

        /// <summary>
        /// Stops timer.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// If the timer has already been disposed.
        /// </exception>
        public void Stop()
        {
            if (!running)
            {
                return;
            }

            running = false;

            OnStopped(EventArgs.Empty);
        }

        #region Callbacks


        long FFramCounter;
        volatile bool FResetCounter;

        // Callback method called by the Win32 multimedia timer when a timer
        // periodic event occurs.
        private void TimerPeriodicEventCallback(int id, int msg, int user, int param1, int param2)
        {
            #region Guard

            if (disposed)
            {
                return;
            }

            #endregion
        }

        // Callback method called by the Win32 multimedia timer when a timer
        // one shot event occurs.
        private void TimerOneShotEventCallback(int id, int msg, int user, int param1, int param2)
        {
            #region Guard

            if (disposed)
            {
                return;
            }

            #endregion
        }

        #endregion

        #region Event Raiser Methods

        // Raises the Disposed event.
        private void OnDisposed(EventArgs e)
        {
            Disposed?.Invoke(this, e);
        }

        // Raises the Started event.
        private void OnStarted(EventArgs e)
        {
            Started?.Invoke(this, e);
        }

        // Raises the Stopped event.
        private void OnStopped(EventArgs e)
        {
            Stopped?.Invoke(this, e);
        }

        #endregion        

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the time between Tick events.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// If the timer has already been disposed.
        /// </exception>   
        public TimeSpan Period
        {
            get
            {
                return FInterval;
            }
            set
            {

                if (value != FInterval)
                {

                    FInterval = value;
                }
            }
        }

        /// <summary>
        /// Gets the timer mode.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// If the timer has already been disposed.
        /// </exception>
        public TimerMode Mode
        {
            get
            {
                return mode;
            }
            set
            {
                if (mode != value)
                {
                    mode = value;

                    if (IsRunning)
                    {
                        Stop();
                        Start();
                    }
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the Timer is running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return running;
            }
        }

        /// <summary>
        /// Returns the time since the timer was created.
        /// Internally uses the .NET Stopwatch.
        /// </summary>
        public TimeSpan Now
        {
            get
            {
                return stopwatch.Elapsed;
            }
        }

        #endregion

        #endregion

        #region IComponent Members

        public event System.EventHandler Disposed;

        public TimeSpan StartedAt { get; private set; }
        public ApartmentState ThreadApartmentState
        {
            get;
            private set;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Frees timer resources.
        /// </summary>
        public void Dispose()
        {
            #region Guard

            if (disposed)
            {
                return;
            }

            #endregion               

            if (running)
            {
                // Stop and destroy timer.
                Stop();
            }

            FDisposable.Dispose();

            disposed = true;

            OnDisposed(EventArgs.Empty);
        }

        #endregion       
    }
}
