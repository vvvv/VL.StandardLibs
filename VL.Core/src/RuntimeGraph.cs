using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace VL.Core
{
    /// <summary>
    /// A collection of methods used by nodes which introduce new entry points into the VL runtime graph.
    /// </summary>
    public static class RuntimeGraph
    {
        static RuntimeGraph()
        {
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            var allHandled = true;
            foreach (var ex in e.Exception.InnerExceptions)
            {
                var re = ex as RuntimeException;
                if (re is null || !ReportException(re))
                    allHandled = false;
            }
            if (allHandled)
                e.SetObserved();
        }

        /// <summary>
        /// Report an exception to the current app host.
        /// </summary>
        /// <param name="exception">The exception to report.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReportException(Exception exception)
        {
            return ReportException(ExceptionDispatchInfo.Capture(exception));
        }

        /// <summary>
        /// Report an exception to the current app host.
        /// </summary>
        /// <param name="exception">The exception to report.</param>
        public static bool ReportException(ExceptionDispatchInfo exception)
        {
            return ReportException(exception, AppHost.CurrentOrGlobal);
        }

        /// <summary>
        /// Report an exception to current app host. If no app host is current the captured app host is used.
        /// </summary>
        /// <param name="exception">The exception to report.</param>
        /// <param name="capturedAppHost">The captured app host.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReportException(Exception exception, AppHost capturedAppHost)
        {
            return ReportException(ExceptionDispatchInfo.Capture(exception), capturedAppHost);
        }

        /// <summary>
        /// Report an exception to current app host. If no app host is current the captured app host is used.
        /// </summary>
        /// <param name="exception">The exception to report.</param>
        /// <param name="capturedAppHost">The captured app host.</param>
        public static bool ReportException(ExceptionDispatchInfo exception, AppHost capturedAppHost)
        {
            var appHost = AppHost.IsAnyCurrent() ? AppHost.Current : capturedAppHost;
            appHost.ReportException(exception);
            return true;
        }
    }
}