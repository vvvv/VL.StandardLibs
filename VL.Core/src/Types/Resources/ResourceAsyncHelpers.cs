using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VL.Lib.Basics.Resources
{
    public static class ResourceAsyncHelpers
    {
        /// <summary>
        /// Fetch a resource, use it, let it get disposed of when the Task is done.
        /// </summary>
        public async static Task<TOut> UsingAsync<T, TOut>(this IResourceProvider<T> source, Func<T, Task<TOut>> worker)
            where T : class, IDisposable
        {
            using (var r = source.GetHandle())
            {
                return await worker(r.Resource);

                // awaiting the task before leaving the using statement
            }
        }

        /// <summary>
        /// Fetches a resource and fires a result when the worker task is done. 
        /// </summary>
        public static IObservable<TOut> ToObservable<T, TOut>(this IResourceProvider<T> source, Func<T, Task<TOut>> worker)
            where T : class, IDisposable
            => Observable.FromAsync(() => source.UsingAsync(worker));

        /// <summary>
        /// Perform an action when a new resource provider gets pushed. The action is performed on the task pool.
        /// </summary>
        public static IObservable<TOut> InBackground<T, TOut>(this IObservable<IResourceProvider<T>> source, Func<T, Task<TOut>> worker)
            where T : class, IDisposable
            => source
            .ObserveOn(TaskPoolScheduler.Default)   // make sure our CPU intensive worker is done on the task pool // or should it be NewThreadScheduler.Default?
            .Select(i => i.UsingAsync(worker))      // for each resource start the worker task
            .Switch()                               // we don't stop previous worker, but just won't observe its result anylonger (switched to observing new task result already)
            .Publish()                              // each consumer shall observe the same observable sequence, no need to do the same work several times.
            .RefCount();                            // manage IConnecteableObservable<TOut> observable (manage Connect() and its returning disposable)

        /// <summary>
        /// Perform an action when a new resource provider gets pushed. The action is performed on the task pool. Check the Cancelation token in your worker to stop work if cancelation was requested.
        /// </summary>        
        public static IObservable<TOut> InBackground<T, TOut>(this IObservable<IResourceProvider<T>> source, Func<T, CancellationToken, Task<TOut>> worker)
            where T : class, IDisposable
            => source
            .ObserveOn(TaskPoolScheduler.Default)
            .Select(i => Observable.FromAsync(cancel => i.UsingAsync(x => worker(x, cancel))))
            .Switch()                               // switch should request cancelation on previous cancelation token when switching to a new job
            .Publish()          
            .RefCount();

        /// <summary>
        /// Retrieves the handle asnychronously.
        /// </summary>
        public static async Task<IResourceHandle<T>> GetHandleAsync<T>(this IResourceProvider<T> source, CancellationToken token) where T : class, IDisposable
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return await Task.Run(() => source.GetHandle(), token);
        }

        /// <summary>
        /// Retrieves the handle asnychronously.
        /// </summary>
        public static async Task<IResourceHandle<T>> GetHandleAsync<T>(this IResourceProvider<T> source, CancellationToken token, int millisecondsRetryDelay) where T : class, IDisposable
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            while (!token.IsCancellationRequested)
            {
                try
                {
                    return source.GetHandle();
                }
                catch (Exception)
                {
                    await Task.Delay(millisecondsRetryDelay, token);
                }
            }
            return null;
        }
    }
}
