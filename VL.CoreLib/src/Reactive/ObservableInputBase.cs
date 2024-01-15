using System;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using VL.Core;
using System.Threading;

namespace VL.Lib.Reactive
{
    /// <summary>
    /// Base class that manages one input observable and one subscription of it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="IDisposable" />
    public abstract class ObservableInputBase<T> : IDisposable
    {
        private readonly object lockingObject = new object();
        private readonly AppHost appHost = AppHost.Current;

        protected SerialDisposable FSubscription = new SerialDisposable();
        protected IObservable<T> FObservable;

        [Obsolete("This field is no longer in use. It's only kept for ABI compatibility.", error: true)]
        protected bool FSubscribing;

        /// <summary>
        /// Subscribes to the observable if not already.
        /// If the input is null only the old subscription is disposed.
        /// </summary>
        /// <param name="observable">The potentially new observable.</param>
        protected void ManageObservable(IObservable<T> observable)
        {
            if (observable != FObservable)
            {
                FObservable = observable;
                try
                {
                    FSubscription.Disposable = observable?.Subscribe(SafeOnNext, SafeOnError, SafeOnCompleted);
                }
                catch (Exception)
                {
                    // Ensure we get out of the current subscription
                    FSubscription.Disposable = null;
                    throw;
                }
            }
        }

        void SafeOnNext(T input)
        {
            lock (lockingObject)
            {
                // While waiting on the lock we could have already been disposed
                if (FSubscription.IsDisposed)
                    return;

                try
                {
                    OnNext(input);
                }
                catch (Exception e)
                {
                    if (RuntimeGraph.RethrowExceptions())
                        throw;

                    RuntimeGraph.ReportException(e, appHost);
                }
            }
        }

        void SafeOnError(Exception e)
        {
            lock (lockingObject)
            {
                OnError(e);
            }
        }

        void SafeOnCompleted()
        {
            lock (lockingObject)
            {
                OnCompleted();
            }
        }

        /// <summary>
        /// Called when a new event is observed. Override in subclass to handle the events.
        /// </summary>
        /// <param name="input">The input.</param>
        protected abstract void OnNext(T input);

        /// <summary>
        /// Called when the upstream observable throws an exception
        /// </summary>
        protected virtual void OnError(Exception e)
        {
            // Set to null so we'll re-subscribe in next frame
            FObservable = null;

            RuntimeGraph.ReportException(e, appHost);
        }

        /// <summary>
        /// Called when the observable is completed
        /// </summary>
        protected virtual void OnCompleted()
        {
            FSubscription.Disposable = null;
        }

        /// <summary>
        /// Releases the subscription. Called on dispose and finalize.
        /// Override in subclass to release more resources.
        /// </summary>
        protected virtual void ReleaseResources()
        {
            FSubscription.Dispose();
        }

        public virtual void Dispose()
        {
            lock (lockingObject)
            {
                ReleaseResources();
            }
        }

        protected void ReleaseState(object state, SynchronizationContext syncContext)
        {
            if (state is IDisposable disposable)
            {
                if (syncContext != null)
                    syncContext.Post(_ =>
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch (Exception e)
                        {
                            // this is our fallback routine.
                            // We are getting called from nowhere, so we need to do this in order toget the exceptions into the Console box
                            // VLSession.Instance.ReportException(e);

                            // or rather get a pink node?
                            RuntimeGraph.ReportException(e, appHost);

                            // it's a bit sad that the user can't catch exceptions happening on state disposal 
                            // with observable catch regions below - as we currently throw them at some point on the sync context,
                            // but not bound to a (next, error, completed)

                            // maybe we could call ReleaseResources OnCompleted? that also should get called from the right thread.

                            // if we move that into OnComppleted ... then also do so for the other observable regions
                        }
                    }
                    , null);
                else
                    disposable.Dispose();
            }
        }
    }
}