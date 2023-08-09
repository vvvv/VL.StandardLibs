using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;
using VL.Lib.Basics.Resources;

namespace VL.Lib.Reactive
{
    /// <summary>
    /// Contains a set of static nodes working on observables.
    /// </summary>
    public static class ObservableNodes
    {
        sealed class _Empty<T>
        {
            public static readonly IObservable<T> Value = Observable.Empty<T>();
        }

        sealed class _Never<T>
        {
            public static readonly IObservable<T> Value = Observable.Never<T>();
        }

        const string Category = "Reactive.Observable";

        /// <summary>
        /// Returns an empty observable sequence
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IObservable<T> Empty<T>() => _Empty<T>.Value;

        /// <summary>
        /// Returns a non-terminating observable sequence, which can be used to denote an infinite duration (e.g. when using reactive joins)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IObservable<T> Never<T>() => _Never<T>.Value;

        /// <summary>
        /// Returns an observable sequence that stays connected to the source as long as there is at least one subscription to the observable sequence
        /// and shares only one single subscription to the underlying sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static IObservable<T> PubRefCount<T>(this IObservable<T> input)
        {
            return input.Publish().RefCount();
        }

        /// <summary>
        /// Terminates the sequence upon successful or exceptional termination
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static IObservable<T> OnErrorTerminate<T>(this IObservable<T> input) => input.OnErrorResumeNext(Observable.Empty<T>());

        /// <summary>
        /// Repeats the source observable sequence until it successfully terminates. Each subsequent subscription will be delayed by the given amount of time in seconds
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="delayInSeconds"></param>
        /// <returns></returns>
        public static IObservable<T> BackoffAndRetry<T>(this IObservable<T> source, double delayInSeconds = 1d)
        {
            var firstAttempt = true;
            return Observable.Defer(() =>
            {
                if (firstAttempt)
                {
                    firstAttempt = false;
                    return source;
                }
                return source.DelaySubscription(TimeSpan.FromSeconds(delayInSeconds));
            }).Retry();
        }

        /// <summary>
        /// Repeats the source observable sequence the specified number of times or until it successfully terminates. Each subsequent subscription will be delayed by the given amount of time in seconds
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="retryCount"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        public static IObservable<T> BackoffAndRetry<T>(this IObservable<T> source, int retryCount = 3, double delay = 1d)
        {
            var firstAttempt = true;
            return Observable.Defer(() =>
            {
                if (firstAttempt)
                {
                    firstAttempt = false;
                    return source;
                }
                return source.DelaySubscription(TimeSpan.FromSeconds(delay));
            }).Retry(retryCount);
        }

        internal static IObservable<T> CatchAndReport<T>(this IObservable<T> source, NodeContext nodeContext)
        {
            var appHost = AppHost.CurrentOrGlobal;
            return source.Catch((Exception e) =>
            {
                var re = new RuntimeException(e, nodeContext);
                RuntimeGraph.ReportException(re, appHost);
                return Observable.Empty<T>();
            });
        }

        /// <summary>
        /// Returns an observable which sets up the <typeparamref name="TState"/> on subscription and will thereafter call the <paramref name="update"/>
        /// method in a loop on a background thread. The speed of the loop can be controlled with the <paramref name="millisecondsDelay"/>.
        /// Notifications can be pushed downstream by using the provided <see cref="IObserver{T}"/> inside the update method.
        /// </summary>
        /// <remarks>
        /// The state will get created and disposed of in the thread which subscribes to the observable while the update of the loop will be called
        /// on a background thread. 
        /// If that is not the desired behavior one can use <see cref="Observable.SubscribeOn{TSource}(IObservable{TSource}, System.Reactive.Concurrency.IScheduler)"/>
        /// to force the creation and destruction of the state to happen on a background thread.
        /// </remarks>
        /// <typeparam name="TState">The type of the state used by the loop.</typeparam>
        /// <typeparam name="T">The type of the notification.</typeparam>
        /// <param name="create">The method which produces the state.</param>
        /// <param name="update">The method which will get called in a loop on a background thread.</param>
        /// <param name="millisecondsDelay">The amount of time to wait before going into another iteration of the loop.</param>
        /// <returns>An observable which when subscribed to runs a loop on a background thread.</returns>
        public static IObservable<T> Loop<TState, T>(Func<TState> create, Func<TState, IObserver<T>, CancellationToken, TState> update, int millisecondsDelay)
            where TState : class
        {
            if (create is null)
                throw new ArgumentNullException(nameof(create));

            if (update is null)
                throw new ArgumentNullException(nameof(update));

            return Observable.Create<T>(async (observer, token) =>
            {
                var state = create();

                try
                {
                    await Task.Run(async () =>
                    {
                        while (!token.IsCancellationRequested)
                        {
                            state = update(state, observer, token);

                            if (millisecondsDelay >= 0)
                                await Task.Delay(millisecondsDelay);
                            else
                                await Task.Yield();
                        }
                    }, token);
                }
                finally
                {
                    if (state is IDisposable disposable)
                        disposable.Dispose();
                }
            });
        }

        /// <summary>
        /// Returns a provider managing the lifetime of the subscription of the upsream observable.
        /// The provider will subscribe when a handle is requested and unsubscribe when the handle gets returned.
        /// </summary>
        /// <typeparam name="T">The type of the notifications.</typeparam>
        /// <param name="source">The upstream observable to subscribe to once a handle is requested.</param>
        /// <returns>A provider managing the lifetime of the upstream observable.</returns>
        public static IResourceProvider<IDisposable> Subscribe<T>(IObservable<T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return ResourceProvider.New(() => source.Subscribe());
        }

        /// <summary>
        /// Returns an observable sequence that depends on the resource given by the upstream provider. The lifetime of the upstream resource
        /// handle will be tied to the resulting observable sequence's lifetime.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource the resulting observable sequence will depend upon.</typeparam>
        /// <typeparam name="T">The type of the notifcations produced by the observable.</typeparam>
        /// <param name="provider">The provider which produces the resource.</param>
        /// <param name="observableFactory">The factory which produces the observable sequence tied to the resource given by the provider.</param>
        /// <returns>An observable sequence depending on the upstream resource.</returns>
        public static IObservable<T> Using<TResource, T>(IResourceProvider<TResource> provider, Func<TResource, IObservable<T>> observableFactory)
            where TResource : class, IDisposable
        {
            if (provider is null)
                throw new ArgumentNullException(nameof(provider));

            if (observableFactory is null)
                throw new ArgumentNullException(nameof(observableFactory));

            return Observable.Using(
                resourceFactory: () => provider.GetHandle(),
                observableFactory: h => observableFactory(h.Resource));
        }

        /// <summary>
        /// Polls the device for a resource on a background thread. Each polled resource will be disposed after all observers have been notified.
        /// </summary>
        /// <typeparam name="TState">The type of the state used by the polling loop.</typeparam>
        /// <typeparam name="TDevice">The type of the device.</typeparam>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <param name="deviceProvider">The device provider to poll for a resource.</param>
        /// <param name="create">The method which produces the state.</param>
        /// <param name="update">The method used for polling.</param>
        /// <param name="millisecondsDelay">The amount of time to wait before polling again.</param>
        /// <returns>An observable of the polled resources.</returns>
        public static IObservable<TResource> PollResource<TState, TDevice, TResource>(
            IResourceProvider<TDevice> deviceProvider, 
            Func<TDevice, TState> create,
            Func<TState, TDevice, CancellationToken, Tuple<TState, TResource>> update,
            int millisecondsDelay)
            where TState : class
            where TDevice : class, IDisposable
            where TResource : IDisposable
        {
            if (deviceProvider is null)
                throw new ArgumentNullException(nameof(deviceProvider));
            if (create is null)
                throw new ArgumentNullException(nameof(create));
            if (update is null)
                throw new ArgumentNullException(nameof(update));

            return Using(
                deviceProvider,
                device =>
                {
                    return Loop<TState, TResource>(
                        create: () => create(device),
                        update: (s, o, t) =>
                        {
                            var (state, resource) = update(s, device, t);
                            using (resource)
                            {
                                if (resource != null)
                                    o.OnNext(resource);
                            }
                            return state;
                        },
                        millisecondsDelay: millisecondsDelay);
                });
        }

        /// <summary>
        /// Polls the device for data on a background thread.
        /// </summary>
        /// <typeparam name="TState">The type of the state used by the polling loop.</typeparam>
        /// <typeparam name="TDevice">The type of the device.</typeparam>
        /// <typeparam name="TData">The type of the data.</typeparam>
        /// <param name="deviceProvider">The device to poll data from.</param>
        /// <param name="create">The method which produces the state.</param>
        /// <param name="update">The method used for polling.</param>
        /// /// <param name="millisecondsDelay">The amount of time to wait before polling again.</param>
        /// <returns>An observable of the polled data.</returns>
        public static IObservable<TData> PollData<TState, TDevice, TData>(
            IResourceProvider<TDevice> deviceProvider, 
            Func<TDevice, TState> create,
            Func<TState, TDevice, CancellationToken, Tuple<TState, TData>> update,
            int millisecondsDelay)
            where TState : class
            where TDevice : class, IDisposable
        {
            if (deviceProvider == null)
                throw new ArgumentNullException(nameof(deviceProvider));
            if (create is null)
                throw new ArgumentNullException(nameof(create));
            if (update == null)
                throw new ArgumentNullException(nameof(update));


            return Using(
                deviceProvider,
                device =>
                {
                    return Loop<TState, TData>(
                        create: () => create(device),
                        update: (s, o, t) =>
                        {
                            var (state, data) = update(s, device, t);
                            o.OnNext(data);
                            return state;
                        },
                        millisecondsDelay: millisecondsDelay);
                });
        }

        /// <summary>
        /// Receives values from an input sequence. Each call to Receive blocks until the next value is produced.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        public class BlockingReceiver<T> : IDisposable
        {
            readonly BlockingCollection<T> FQueue = new BlockingCollection<T>();
            readonly SerialDisposable FSubscription = new SerialDisposable();
            IObservable<T> FObservable;

            /// <summary>
            /// Blocks until the next item is produced by the input sequence and returns the value.
            /// Since the call is blocking make sure to use it on a different thread than the producer and provide it with a proper cancellation token.
            /// Without a token the method will return immediately with the default value.
            /// </summary>
            /// <param name="input">The sequence to receive values from.</param>
            /// <param name="defaultValue">The value to return in case the receive call fails.</param>
            /// <param name="cancellationToken">The cancellation token to cancel the blocking receive call.</param>
            /// <param name="success">Whether or not a value was received.</param>
            /// <returns>The received value.</returns>
            public T Receive(IObservable<T> input, T defaultValue, CancellationToken cancellationToken, out bool success)
            {
                if (input != FObservable)
                {
                    FObservable = input;
                    FSubscription.Disposable = input.Subscribe(v => FQueue.Add(v));
                }

                if (cancellationToken != CancellationToken.None)
                {
                    try
                    {
                        success = true;
                        return FQueue.Take(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Token was cancelled
                        success = false;
                        return defaultValue;
                    }
                    catch (ObjectDisposedException)
                    {
                        // Token was disposed or our queue was disposed
                        success = false;
                        return defaultValue;
                    }
                }
                else
                {
                    // No token given, do nothing.
                    success = false;
                    return defaultValue;
                }
            }

            void IDisposable.Dispose()
            {
                FSubscription.Dispose();
                FQueue.Dispose();
            }
        }
    }
}
