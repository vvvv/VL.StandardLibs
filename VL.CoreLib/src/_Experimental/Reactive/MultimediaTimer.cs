using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Animation;
using VL.Lib.Reactive;

[assembly: ImportType(typeof(MultimediaTimerObservable), Name = "MultimediaTimer", Category = "Reactive.Advanced.Experimental")]

namespace VL.Lib.Reactive
{
    /// <summary>
    /// Observable timer on a backround thread using the windows multimedia timer with about 1ms precision.
    /// </summary>
    /// <remarks>
    /// You should only use one multimedia timer at a time!
    /// </remarks>
    [ProcessNode(FragmentSelection = FragmentSelection.Explicit)]
    public class MultimediaTimerObservable
    {
        readonly Subject<TimerClock> FSubject;
        readonly SingleAssignmentDisposable FDisposable = new SingleAssignmentDisposable();
        readonly IObservable<TimerClock> FResult;
        readonly TimerClock FFrameClock = new TimerClock();

        /// <summary>
        /// Updates the timer properties.
        /// </summary>
        /// <param name="period">The period time in milliseconds.</param>
        /// <param name="mode"></param>
        /// <param name="resetCounter">If set to <c>true</c> the frame counter will be reset.</param>
        /// <param name="enabled">If set to <c>true</c> the timer runs.</param>
        /// <param name="lastPeriod">Last measured period time in milliseconds.</param>
        /// <returns>Observable of a TimerClock that can be used as frame clock input for animation nodes.</returns>
        [Fragment]
        public IObservable<TimerClock> Update(out float lastPeriod, int period = 100, TimerMode mode = TimerMode.Periodic, bool resetCounter = false, bool enabled = true)
        {
            this.Mode = mode;
            this.Period = period;
            FResetCounter |= resetCounter;

            if (enabled != running)
            {
                if (enabled)
                    Start();
                else
                    Stop();
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

        #region Win32 Multimedia Timer Functions

        // Gets timer capabilities.
        [DllImport("winmm.dll")]
        private static extern int timeGetDevCaps(ref TimerCaps caps, int sizeOfTimerCaps);

        // Creates and starts the timer.
        [DllImport("winmm.dll")]
        private static extern int timeSetEvent(int delay, int resolution, TimeProc proc, IntPtr user, int mode);

        // Stops and destroys the timer.
        [DllImport("winmm.dll")]
        private static extern int timeKillEvent(int id);

        // Indicates that the operation was successful.
        private const int TIMERR_NOERROR = 0;

        #endregion

        #region Fields

        // Timer identifier.
        private int timerID;

        // Timer mode.
        private volatile TimerMode mode;

        // Period between timer events in milliseconds.
        private volatile int period;

        // Called by Windows when a timer periodic event occurs.
        private TimeProc timeProcPeriodic;

        // Called by Windows when a timer one shot event occurs.
        private TimeProc timeProcOneShot;

        // Indicates whether or not the timer is running.
        private volatile bool running = false;

        // Indicates whether or not the timer has been disposed.
        private volatile bool disposed = false;

        // For implementing IComponent.
        private ISite site = null;

        // Multimedia timer capabilities.
        private static TimerCaps caps;

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
        /// Initialize class.
        /// </summary>
        static MultimediaTimerObservable()
        {
            // Get multimedia timer capabilities.
            timeGetDevCaps(ref caps, Marshal.SizeOf(caps));
        }


        /// <summary>
        /// Initializes a new instance of the Timer class.
        /// </summary>
        [Fragment]
        public MultimediaTimerObservable()
        {

            FSubject = new Subject<TimerClock>();
            FDisposable.Disposable = FSubject;
            FResult = FSubject.PubRefCount();

            Initialize();
        }

        // Initialize timer with default values.
        private void Initialize()
        {
            this.mode = TimerMode.Periodic;
            this.period = Capabilities.periodMin;

            running = false;

            timeProcOneShot = new TimeProc(TimerOneShotEventCallback);
            timeProcPeriodic = new TimeProc(TimerPeriodicEventCallback);

            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        #endregion

        #region Methods

        TimeSpan FOldTime;
        TimeSpan FCallbackAccuracy = TimeSpan.FromTicks((long)Math.Ceiling(1.5 * TimeSpan.TicksPerMillisecond));

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

            //// start thread which does the timer work
            var thread = new Thread(TimerFunctionStart);
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.AboveNormal;
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

        private void TimerFunctionStart()
        {
            StartedAt = stopwatch.Elapsed;

            // If the periodic event callback should be used.
            if (Mode == TimerMode.Periodic)
            {
                // Create and start timer.
                timerID = timeSetEvent(Period, 0, timeProcPeriodic, IntPtr.Zero, (int)Mode);
            }
            // Else the one shot event callback should be used.
            else
            {
                // Create and start timer.
                timerID = timeSetEvent(Period, 0, timeProcOneShot, IntPtr.Zero, (int)Mode);
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

            #region Guard

            if (!running)
            {
                return;
            }

            #endregion


            // Stop and destroy timer.
            int result = timeKillEvent(timerID);

            Debug.Assert(result == TIMERR_NOERROR);

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

            var t = stopwatch.Elapsed;
            var interval = t - FOldTime;
            LastIntervalDiff = interval - LastInterval;
            LastInterval = interval;
            FOldTime = t;

            if (FResetCounter)
            {
                FFramCounter = 0;
                FResetCounter = false;
            }
            FFrameClock.Time = t.TotalSeconds;
            FFrameClock.TimeDifference = interval.TotalSeconds;
            FFrameClock.FrameCounter = FFramCounter++;
            FSubject.OnNext(FFrameClock);
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

            var t = stopwatch.Elapsed;
            var interval = t - FOldTime;
            LastIntervalDiff = interval - LastInterval;
            LastInterval = interval;
            FOldTime = t;
            FFrameClock.Time = t.TotalSeconds;
            FFrameClock.TimeDifference = interval.TotalSeconds;
            FFrameClock.FrameCounter = FFramCounter++;
            FSubject.OnNext(FFrameClock);
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
        public int Period
        {
            get
            {
                return period;
            }
            set
            {
                #region Require

                if (value != period)
                {
                    if (value < Capabilities.periodMin || value > Capabilities.periodMax)
                    {
                        throw new ArgumentOutOfRangeException("Period", value,
                            "Multimedia Timer period out of range.");
                    }

                    #endregion

                    period = value;

                    if (IsRunning)
                    {
                        Stop();
                        Start();
                    }
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

        /// <summary>
        /// Gets the timer capabilities.
        /// </summary>
        public static TimerCaps Capabilities
        {
            get
            {
                return caps;
            }
        }

        #endregion

        #endregion

        #region IComponent Members

        public event System.EventHandler Disposed;

        public ISite Site
        {
            get
            {
                return site;
            }
            set
            {
                site = value;
            }
        }

        public TimeSpan StartedAt { get; private set; }

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

            disposed = true;

            if (running)
            {
                // Stop and destroy timer.
                Stop();
            }

            FDisposable.Dispose();

            OnDisposed(EventArgs.Empty);
        }

        #endregion       
    }

    /// <summary>
    /// Represents information about the multimedia Timer's capabilities.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TimerCaps
    {
        /// <summary>
        /// Minimum supported period in milliseconds.
        /// </summary>
        public int periodMin;

        /// <summary>
        /// Maximum supported period in milliseconds.
        /// </summary>
        public int periodMax;

        public static TimerCaps Default
        {
            get
            {
                return new TimerCaps { periodMin = 1, periodMax = Int32.MaxValue };
            }
        }
    }

    /// <summary>
    /// The exception that is thrown when a timer fails to start.
    /// </summary>
    public class TimerStartException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the TimerStartException class.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception. 
        /// </param>
        public TimerStartException(string message) : base(message)
        {
        }
    }
}
