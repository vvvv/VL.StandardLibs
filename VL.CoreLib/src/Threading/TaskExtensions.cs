using System;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;

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

        public static void DisposeAfterCompletion<T>(this Task<T> task) where T : IDisposable
        {
            if (task.IsCompleted)
            {
                if (task.IsCompletedSuccessfully)
                    task.Result?.Dispose();
            }
            else
            {
                _ = task.ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                        t.Result?.Dispose();
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        public static T WaitWhilePumpingMessageLoop<T>(this T task) where T : Task
        {
            var platformServices = AppHost.Current.Services.GetService<IPlatformServices>();
            while (!task.Wait(millisecondsTimeout: 1))
            {
                platformServices?.DoEvents();
            }
            return task;
        }
    }
}
