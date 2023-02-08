using System;
using System.Threading;
using System.Threading.Tasks;

namespace VL.Lib.Threading
{
    /// <summary>
    /// Extension methods for tasks.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Cancels the tasks, waits for it to finish and disposes both the cancellation token source as well as the task.
        /// </summary>
        /// <param name="task">The task to cancel.</param>
        /// <param name="cancellation">The cancellation token source whose token is used by the task.</param>
        /// <param name="timeout">The time in milli seconds to wait for termination of the task.</param>
        public static void CancelAndDispose(this Task task, CancellationTokenSource cancellation, int timeout)
        {
            // Cancel the current task
            cancellation.Cancel();
            // Wait for it to finish
            try
            {
                if (task.Wait(timeout))
                    task.Dispose();
            }
            catch (AggregateException)
            {
                // Fine
            }
            cancellation.Dispose();
        }
    }
}
