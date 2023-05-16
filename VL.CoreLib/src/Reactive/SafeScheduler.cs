using System;
using System.Reactive.Concurrency;
using System.Threading;
using VL.Core;

namespace VL.Lib.Reactive
{
    /// <summary>
    /// Provides wrapped versions of all the standard schedulers of RX with VL exception handling.
    /// </summary>
    public static class SafeScheduler
    {
        [ThreadStatic]
        static IScheduler FCurrentSynchronizationContext, FCurrentThread, FImmediate;

        /// <summary>
        /// Gets a scheduler that schedules work as soon as possible on the current thread.
        /// </summary>
        public static IScheduler CurrentSynchronizationContext
        {
            get
            {
                if (FCurrentSynchronizationContext == null)
                {
                    var context = SynchronizationContext.Current;
                    if (context != null)
                        FCurrentSynchronizationContext = new SynchronizationContextScheduler(context).CatchAndReportRuntimeExceptions();
                    else
                        FCurrentSynchronizationContext = Default;
                }
                return FCurrentSynchronizationContext;
            }
        }

        /// <summary>
        /// Gets a scheduler that schedules work on the platform's default scheduler.
        /// </summary>
        public static readonly IScheduler Default = Scheduler.Default.CatchAndReportRuntimeExceptions();

        /// <summary>
        /// Gets a scheduler that schedules work as soon as possible on the current thread.
        /// </summary>
        public static IScheduler CurrentThread => FCurrentThread ?? (FCurrentThread = Scheduler.CurrentThread.CatchAndReportRuntimeExceptions());

        /// <summary>
        /// Gets a scheduler that schedules work immediately on the current thread.
        /// </summary>
        public static readonly IScheduler Immediate = FImmediate ?? (FImmediate = Scheduler.Immediate.CatchAndReportRuntimeExceptions());

        /// <summary>
        /// Gets a scheduler that schedules work on a new thread using default thread creation options.
        /// </summary>
        public static readonly IScheduler NewThread = NewThreadScheduler.Default.CatchAndReportRuntimeExceptions();

        /// <summary>
        /// Gets a scheduler that schedules work on Task Parallel Library (TPL) task pool using the default TaskScheduler.
        /// </summary>
        public static readonly IScheduler TaskPool = TaskPoolScheduler.Default.CatchAndReportRuntimeExceptions();

        /// <summary>
        /// Gets a scheduler that schedules work on the thread pool.
        /// </summary>
        public static readonly IScheduler ThreadPool = ThreadPoolScheduler.Instance.CatchAndReportRuntimeExceptions();

        /// <summary>
        /// Returns a scheduler that wraps the original scheduler, adding VL exception handling for scheduled actions.
        /// </summary>
        public static IScheduler CatchAndReportRuntimeExceptions(this IScheduler scheduler)
        {
            var appHost = AppHost.Current;
            return scheduler.Catch<Exception>(e =>
            {
                RuntimeGraph.ReportException(e, appHost);
                return true;
            });
        }

    }
}
