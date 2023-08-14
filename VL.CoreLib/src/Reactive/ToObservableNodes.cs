using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using VL.Core;
using VL.Lib.Collections;

namespace VL.Lib.Reactive
{
    public abstract class ToObservableBase<TObservable, TMessage> : IDisposable
    {
        readonly Subject<TObservable> FSubject;
        readonly IObservable<TObservable> FOutput;
        int FUpdateCount;
        bool FHasPendingMessage;
        TMessage FPendingMessage;

        public ToObservableBase()
        {
            FSubject = new Subject<TObservable>();
            FOutput = Observable.Create<TObservable>(observer =>
            {
                var subscription = FSubject.Subscribe(observer);
                // If update has been called only once so far send the pending message
                if (FUpdateCount == 1 && FHasPendingMessage)
                {
                    OnNext(observer, FPendingMessage);
                }
                return subscription;
            });
        }

        public virtual IObservable<TObservable> Update(TMessage message, bool send)
        {
            var updateCount = Interlocked.Increment(ref FUpdateCount);

            if (send && !FSubject.IsDisposed)
            {
                // If we're in the first update just store the message and send on subscription in same frame
                if (updateCount > 1)
                    OnNext(FSubject, message);
                else
                {
                    FPendingMessage = Store(message);
                    FHasPendingMessage = true;
                }
            }

            return FOutput;
        }

        protected abstract TMessage Store(TMessage message);
        protected abstract void OnNext(IObserver<TObservable> subject, TMessage message);

        public void Dispose()
        {
            FSubject.Dispose();
        }
    }

    /// <summary>
    /// Creates an observable and sends the input value as an event on it. The output observable is always the same and never completes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ToObservable<T> : ToObservableBase<T, T>
    {
        public override IObservable<T> Update(T message, bool send)
        {
            return base.Update(message, send);
        }

        protected override void OnNext(IObserver<T> subject, T message)
        {
            subject.OnNext(message);
        }

        protected override T Store(T message)
        {
            return message;
        }
    }

    //old version that was exactly like the other two
    ///// <summary>
    ///// Creates an observable and sends the input value as an event on it. The output observable is always the same and never completes
    ///// </summary>
    ///// <typeparam name = "T" ></ typeparam >
    //public class ToObservable<T> : IDisposable
    //{
    //    readonly BehaviorSubject<IObservable<T>> FSubject;
    //    readonly SingleAssignmentDisposable FDisposable = new SingleAssignmentDisposable();
    //    readonly IObservable<T> FResult;

    //    public ToObservable()
    //    {
    //        FSubject = new BehaviorSubject<IObservable<T>>(Observable.Empty<T>());
    //        FDisposable.Disposable = FSubject;
    //        FResult = FSubject.Switch().PubRefCount();
    //    }

    //    public IObservable<T> Update(T message, bool send)
    //    {
    //        if (send && !FDisposable.IsDisposed)
    //        {
    //            FSubject.OnNext(Observable.Return(message));
    //        }

    //        return FResult;
    //    }

    //    public void Dispose()
    //    {
    //        FDisposable.Dispose();
    //    }
    //}

    /// <summary>
    /// Routes the input observable to the output whenever 'Send' is true, the output observable is always the same and never completes.
    /// It also catches exceptions thrown by the input observable and throws them in its update operation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ToObservableSwitch<T> : IDisposable
    {
        readonly BehaviorSubject<IObservable<T>> FSubject;
        readonly SingleAssignmentDisposable FDisposable = new SingleAssignmentDisposable();
        IObservable<T> FResult;
        IObservable<T> FEmpty = Observable.Empty<T>();

        public ToObservableSwitch()
        {
            FSubject = new BehaviorSubject<IObservable<T>>(Observable.Empty<T>());
            FDisposable.Disposable = FSubject;
            SetupResultObservable();
        }

        private void SetupResultObservable()
        {
            var switched = FSubject.Switch();
            var appHost = AppHost.Current;
            FResult = switched.Catch((Exception e) =>
            {
                FLastException = e;
                RuntimeGraph.ReportException(e, appHost);
                return Observable.Empty<T>(); // Terminate, next Update will setup a new observable
            }).PubRefCount();
        }

        Exception FLastException;
        IObservable<T> FLastInput;

        public IObservable<T> Update(IObservable<T> message, bool send)
        {
            if (FLastException != null && FLastInput != message)
            {
                //retry with new input observable
                UnsubscribeFromInput();
                SetupResultObservable();
            }

            FLastException = null;

            if (send && !FDisposable.IsDisposed)
            {
                FSubject.OnNext(message);
                FLastInput = message;
            }

            return FResult;
        }

        public void Dispose()
        {
            UnsubscribeFromInput();
            FDisposable.Dispose();
        }

        private void UnsubscribeFromInput()
        {
            if (!FSubject.IsDisposed)
            {
                FSubject.OnNext(FEmpty);
            }
        }
    }

    ///// <summary>
    ///// Creates an observable and sends each element in the input sequence as individual events on it. The output observable is always the same and never completes
    ///// </summary>
    ///// <typeparam name="T"></typeparam>
    //[HotSwap] // Tells hot swap to call ctor - too hard to save whole observable graph
    //public class ToObservableSequence<T> : IDisposable
    //{
    //    readonly BehaviorSubject<IObservable<T>> FSubject;
    //    readonly SingleAssignmentDisposable FDisposable = new SingleAssignmentDisposable();
    //    readonly IObservable<T> FResult;

    //    public ToObservableSequence()
    //    {
    //        FSubject = new BehaviorSubject<IObservable<T>>(Observable.Empty<T>());
    //        FDisposable.Disposable = FSubject;
    //        FResult = FSubject.Switch().PubRefCount();
    //    }

    //    public IObservable<T> Update(IEnumerable<T> messages, bool send = true)
    //    {
    //        if(send && !FDisposable.IsDisposed)
    //        {
    //            FSubject.OnNext(messages.ToObservable());
    //        }

    //        return FResult;
    //    }

    //    public void Dispose()
    //    {
    //        FDisposable.Dispose();
    //    }
    //}

    /// <summary>
    /// Creates an observable and sends the all elements of the input as an event on it. The output observable is always the same and never completes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ToObservableSequence<T> : ToObservableBase<T, IEnumerable<T>>
    {
        public override IObservable<T> Update(IEnumerable<T> messages, bool send = true)
        {
            return base.Update(messages, send);
        }

        protected override void OnNext(IObserver<T> subject, IEnumerable<T> message)
        {
            foreach (var m in message)
                subject.OnNext(m);
        }

        protected override IEnumerable<T> Store(IEnumerable<T> message)
        {
            return message.ToSpread();
        }
    }
}
