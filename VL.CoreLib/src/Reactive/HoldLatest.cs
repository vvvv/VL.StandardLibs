using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Lib.Collections;

namespace VL.Lib.Reactive
{
    /// <summary>
    /// Stores the latest notification and outputs it. Use S+H [Reactive] if you want to sample and hold all notifications between two calls
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    public class HoldLatest<TIn> : ObservableInputBase<TIn>
    {
        readonly Using<TIn> FOutput = new Using<TIn>();
        readonly TIn FInitialResult;

        bool FOnData;

        public HoldLatest(TIn initialResult)
        {
            FOutput.Resource = initialResult;
            FInitialResult = initialResult;
        }

        public void Update(IObservable<TIn> asyncNotifications, bool reset)
        {
            ManageObservable(asyncNotifications);

            if (reset)
                FOutput.Resource = FInitialResult;
        }

        public void GetData(out TIn value, out bool onData)
        {
            onData = FOnData;
            value = FOutput.Resource;
            FOnData = false;
        }

        protected override void OnNext(TIn input)
        {         
            FOutput.Resource = input;
            FOnData = true;
        }

        protected override void ReleaseResources()
        {
            base.ReleaseResources();
            FOutput.Dispose();
        }
    }

    /// <summary>
    /// Sample and hold all notifications between two calls. Use HoldLatest [Reactive] if you only need the newest notification
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SampleAndHold<T> : IDisposable
    {
        IObservable<T> FAsyncNotifications;
        IEnumerator<IList<T>> FEnumerator;
        Spread<T> FLastFrameSpread;

        public void Dispose()
        {
            FEnumerator?.Dispose();
        }

        public void Sample(IObservable<T> asyncNotifications, out Spread<T> notifications, out bool onData)
        {
            if (asyncNotifications != FAsyncNotifications)
            {
                FEnumerator?.Dispose();
                FAsyncNotifications = asyncNotifications;
                FEnumerator = asyncNotifications?.Chunkify().GetEnumerator();
            }

            onData = false;
            if (FEnumerator != null && FEnumerator.MoveNext())
            {
                var current = FEnumerator.Current;
                if (current.Count > 0)
                {
                    FLastFrameSpread = current.ToSpread();
                    onData = true; 
                }
            }

            notifications = FLastFrameSpread ?? Spread<T>.Empty;
        }
    }
}