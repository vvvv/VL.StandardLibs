using System;
using System.Reactive.Linq;
using System.Threading;
using VL.Core;

namespace VL.Lib.Reactive
{
    /// <summary>
    /// Keeps track of the calls made internally in the obserable computation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DebugNode<T>
    {
        IObservable<T> FInput, FOutput;
        int FInputChangedCount, FSubscribeCalls, FUnsubscribeCalls, FOnNextCalls, FOnErrorCalls, FOnCompletedCalls, FLatestThreadId;

        public IObservable<T> Update(IObservable<T> input, bool reset, out int inputChangedCount, out int subscribeCalls, out int onNextCalls, out int onErrorCalls, out int onCompletedCalls, out int unsubscribeCalls, out int latestThreadId)
        {
            if (reset)
                ResetCallCounts();

            if (input != FInput)
            {
                FInput = input;

                FInputChangedCount++;
                ResetCallCounts();

                FOutput = Observable.Create<T>((observer) =>
                {
                    if (input == FInput)
                        FSubscribeCalls++;
                    var subscription = FInput.Subscribe(
                        _ =>
                        {
                            if (input == FInput)
                            {
                                FOnNextCalls++;
                                FLatestThreadId = Thread.CurrentThread.ManagedThreadId;
                            }
                            observer.OnNext(_);
                        },
                        _ =>
                        {
                            if (input == FInput)
                            {
                                FOnErrorCalls++;
                                FLatestThreadId = Thread.CurrentThread.ManagedThreadId;
                            }
                            observer.OnError(_);
                        },
                        () =>
                        {
                            if (input == FInput)
                            {
                                FOnCompletedCalls++;
                                FLatestThreadId = Thread.CurrentThread.ManagedThreadId;
                            }
                            observer.OnCompleted();
                        });
                    return () =>
                    {
                        if (input == FInput)
                            FUnsubscribeCalls++;
                        subscription.Dispose();
                    };
                });
            }

            inputChangedCount = FInputChangedCount;
            subscribeCalls = FSubscribeCalls;
            unsubscribeCalls = FUnsubscribeCalls;
            onNextCalls = FOnNextCalls;
            onErrorCalls = FOnErrorCalls;
            onCompletedCalls = FOnCompletedCalls;
            latestThreadId = FLatestThreadId;

            return FOutput;
        }

        private void ResetCallCounts()
        {
            FSubscribeCalls = 0;
            FUnsubscribeCalls = 0;
            FOnNextCalls = 0;
            FOnErrorCalls = 0;
            FOnCompletedCalls = 0;
            FLatestThreadId = 0;
        }
    }
}
