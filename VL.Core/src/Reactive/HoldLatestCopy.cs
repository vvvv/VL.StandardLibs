using System;
using System.Reactive.Disposables;
using System.Threading;
using VL.Core;

namespace VL.Lib.Reactive
{
    /// <summary>
    /// Holds on to a copy of the latest received data. Whenever data gets pushed to the node a copy is made and stored internally.
    /// </summary>
    public abstract class HoldLatestCopy<T, TCopy> : IDisposable
        where TCopy : T
    {
        readonly SerialDisposable FSubscription = new SerialDisposable();
        readonly AutoResetEvent FProduceNewDataEvent = new AutoResetEvent(true);
        IObservable<T> FAsyncNotifications;
        readonly TCopy FInitial;
        TCopy FFront, FBack;
        bool FHasNewData;
        int FDropCount, FSwapCount, FTimeout;

        public HoldLatestCopy(T initialResult)
        {
            CopyTo(initialResult, ref FInitial);
        }

        public virtual T Update(IObservable<T> asyncNotifications, int timeout, bool reset, out int swapCount, out int dropCount)
        {
            if (asyncNotifications != FAsyncNotifications || timeout != FTimeout)
            {
                FTimeout = timeout;
                FAsyncNotifications = asyncNotifications;
                FSubscription.Disposable = asyncNotifications.Subscribe(data =>
                {
                    // Wait until buffers have been swapped by main thread
                    if (FProduceNewDataEvent.WaitOne(timeout))
                    {
                        CopyTo(data, ref FBack);
                        FHasNewData = true;
                    }
                    else
                    {
                        // Timed out
                        FDropCount++;
                        if (!FHasNewData)
                            FProduceNewDataEvent.Set();
                    }
                });
            }

            if (FHasNewData)
            {
                // Do the swap
                FHasNewData = false;
                var t = FFront;
                FFront = FBack;
                FBack = t;
                FSwapCount++;

                // Allow new data to come in
                FProduceNewDataEvent.Set();
            }

            if (reset)
                CopyTo(FInitial, ref FFront);

            swapCount = FSwapCount;
            dropCount = FDropCount;

            if (swapCount > 0)
                return FFront;
            else
                return FInitial;
        }

        public void Dispose()
        {
            FSubscription.Dispose();
            FProduceNewDataEvent.Dispose();
        }

        protected abstract void CopyTo(T source, ref TCopy destination);
    }
}
