using System;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;
using VL.Lib.Animation;
using VL.Lib.Threading;

namespace VL.Lib.Reactive
{
    /// <summary>
    /// Runs an infinite loop in a background thread.
    /// </summary>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    public class AsyncLoop<TState, TOut> : IDisposable
        where TState : class
    {
        const int Timeout = 1000;
        readonly Subject<TOut> FSubject = new Subject<TOut>();
        TState FState;
        bool FShouldRun = true;
        Func<TState> FCreate;
        Func<TState, CancellationToken, Tuple<TState, TOut, bool>> FUpdate;
        CancellationTokenSource FCancellation;
        Task FCurrentTask;

        public IObservable<TOut> Update(Func<TState> create, Func<TState, CancellationToken, Tuple<TState, TOut, bool>> update, bool restart, bool abort, out bool inProgress)
        {
            FCreate = create;
            FUpdate = update;

            if (restart)
                Stop(shouldRun: true);

            if (abort)
                Stop(shouldRun: false);

            if (FShouldRun && FCurrentTask == null)
            {
                FCancellation = new CancellationTokenSource();
                var token = FCancellation.Token;
                try
                {
                    FCurrentTask = Task.Run(() =>
                    {
                        var st = Stopwatch.StartNew();
                        var frameClock = new FrameClock();
                        using var _ = Clocks.SetCurrentFrameClock(frameClock);

                        while (!token.IsCancellationRequested)
                        {
                            try
                            {
                                frameClock.SetFrameTime(new Time(st.Elapsed.TotalSeconds));

                                var state = FState ?? FCreate?.Invoke();
                                if (state != null && FUpdate != null)
                                {
                                    var result = FUpdate(state, token);
                                    state = result.Item1;
                                    var output = result.Item2;
                                    if (result.Item3)
                                        break;
                                    try
                                    {
                                        FSubject.OnNext(output);
                                    }
                                    catch (ObjectDisposedException)
                                    {
                                        break;
                                    }
                                }
                                FState = state;
                            }
                            catch (RuntimeException e)
                            {
                                var canceled = e.Original as OperationCanceledException;
                                if (canceled != null && canceled.CancellationToken == token)
                                    break;
                                else
                                    throw;
                            }
                        }

                        (FState as IDisposable)?.Dispose();
                        FState = null;
                    }, token);
                }
                catch (OperationCanceledException)
                {
                    // Fine
                }
                catch (ObjectDisposedException)
                {
                    // Fine
                }
            }

            if (FCurrentTask != null)
                inProgress = !FCurrentTask.IsCompleted;
            else
                inProgress = false;

            var exception = FCurrentTask?.Exception;
            if (exception != null)
            {
                FCurrentTask = null;
                throw exception;
            }

            return FSubject;
        }

        void Stop(bool shouldRun)
        {
            FCurrentTask?.CancelAndDispose(FCancellation, Timeout);
            FCurrentTask = null;
            FShouldRun = shouldRun;
        }

        void IDisposable.Dispose()
        {
            try
            {
                // On shutdown wait until the background task is done, on hotswap wait only for a very short time.
                FCurrentTask?.CancelAndDispose(FCancellation, Timeout);
                FCurrentTask = null;
                FSubject.Dispose();
            }
            catch (Exception)
            {
                // Ignore
            }
        }
    }

    /// <summary>
    /// Runs the given task once in a background thread.
    /// </summary>
    /// <typeparam name="TOut">The output type.</typeparam>
    public class AsyncTask<TOut> : IDisposable
    {
        readonly Subject<TOut> FSubject = new Subject<TOut>();
        CancellationTokenSource FCancellation;
        Task FCurrentTask;

        public IObservable<TOut> Update<TState>(Func<TState> create, Func<TState, CancellationToken, Tuple<TState, TOut>> update, bool trigger, bool abort, out bool inProgress)
            where TState : class
        {
            if (abort || trigger)
            {
                FCurrentTask?.CancelAndDispose(FCancellation, 1);
                FCurrentTask = null;
            }

            if (trigger)
            {
                FCancellation = new CancellationTokenSource();
                var token = FCancellation.Token;
                try
                {
                    FCurrentTask = Task.Run(() =>
                    {
                        var state = create();
                        var disposable = state as IDisposable;
                        try
                        {
                            var result = update(state, token);
                            disposable = result.Item1 as IDisposable;
                            if (!token.IsCancellationRequested)
                                FSubject.OnNext(result.Item2);
                        }
                        catch (RuntimeException e)
                        {
                            var canceled = e.Original as OperationCanceledException;
                            if (canceled != null && canceled.CancellationToken == token)
                                return;
                            else
                                throw;
                        }
                        finally
                        {
                            disposable?.Dispose();
                        }
                    }, token);
                }
                catch (OperationCanceledException)
                {
                    // Fine
                }
                catch (ObjectDisposedException)
                {
                    // Fine
                }
            }

            if (FCurrentTask != null)
                inProgress = !FCurrentTask.IsCompleted;
            else
                inProgress = false;

            var exception = FCurrentTask?.Exception;
            if (exception != null)
            {
                FCurrentTask = null;
                throw exception;
            }

            return FSubject;
        }

        void IDisposable.Dispose()
        {
            try
            {
                FCurrentTask?.CancelAndDispose(FCancellation, 1000);
                FCurrentTask = null;
                FSubject.Dispose();
            }
            catch (Exception)
            {
                // Ignore
            }
        }
    }
}
