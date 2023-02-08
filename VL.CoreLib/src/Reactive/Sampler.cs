using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using VL.Core;
using System.Reactive.Concurrency;
using System.Threading;
using VL.Lib.Collections;

namespace VL.Lib.Reactive
{
    public static class ExperimentalObservableNodes
    {
        public static IObservable<object> Merge(this IObservable<object> input, IObservable<object> input2)
        {
            return Observable.Merge(input, input2);
        }
    }

    /// <summary>
    /// Samples all events that happen between the last update and the current update and outputs the event values as a spread. The output is an empty spread if no events have occured
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Sampler<T> : IDisposable
    {
        public static readonly Sampler<T> Default = new Sampler<T>();

        readonly IObservable<T> FAsyncNotifications;
        IEnumerator<IList<T>> FEnumerator;

        private Sampler()
            : this(Observable.Empty<T>())
        {
        }

        private Sampler(IObservable<T> input)
        {
            FAsyncNotifications = input;
            FEnumerator = input.BackoffAndRetry(delayInSeconds: 1d) // Re-subscribe on error so we don't experience dead observable chains
                .Chunkify()
                .GetEnumerator();
        }

        public void Dispose()
        {
            if (this != Default)
            {
                FEnumerator?.Dispose();
                FEnumerator = null;
            }
        }

        public Sampler<T> Sample(IObservable<T> asyncNotifications, out Spread<T> notifications, out bool onData)
        {
            if (asyncNotifications != FAsyncNotifications)
            {
                Dispose();
                return new Sampler<T>(asyncNotifications).Sample(asyncNotifications, out notifications, out onData);
            }

            notifications = Spread<T>.Empty;

            if (FEnumerator != null)
            {
                if (FEnumerator.MoveNext())
                    notifications = FEnumerator.Current.ToSpread();
                else
                    Dispose();
            }

            onData = notifications.Count > 0;

            return this;
        }
    }
}
