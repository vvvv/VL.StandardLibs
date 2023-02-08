using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using VL.Core;

namespace VL.Lib.Reactive
{
    /// <summary>
    /// Keeps a single subscription of the input observable so that it will be kept alive and passes the events on to the output. The observable on the output is always the same
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class KeepAlive<T> : IDisposable
    {
        protected IObservable<T> FObservable;
        readonly BehaviorSubject<IObservable<T>> FSubject;
        SingleAssignmentDisposable FSubscription = new SingleAssignmentDisposable();
        readonly SingleAssignmentDisposable FDisposable = new SingleAssignmentDisposable();
        readonly IObservable<T> FResult;

        public KeepAlive()
        {
            FSubject = new BehaviorSubject<IObservable<T>>(Observable.Empty<T>());
            FDisposable.Disposable = FSubject;
            FResult = FSubject.Switch().PubRefCount();
            FSubscription.Disposable = FResult.Subscribe();
        }

        public IObservable<T> Update(IObservable<T> message, bool enabled = true)
        {
            if(enabled)
            {
                if (FSubscription.IsDisposed)
                {
                    FSubscription = new SingleAssignmentDisposable();
                    FSubscription.Disposable = FResult.Subscribe();
                }
            }
            else
            {
                if (!FSubscription.IsDisposed)
                    FSubscription.Dispose();
            }

            if (FObservable != message && !FDisposable.IsDisposed)
            {
                if (!FSubscription.IsDisposed)
                    FSubscription.Dispose();

                FObservable = message; 
                FSubject.OnNext(message);
            }

            return FObservable;
        }

        public void Dispose()
        {
            FSubscription.Dispose();
            FDisposable.Dispose();
        }
    }
}
