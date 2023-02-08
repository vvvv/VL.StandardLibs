using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using VL.Core;
using VL.Core.CompilerServices;

namespace VL.Lib.Reactive
{
    interface IFeedState
    {
        void SwapState(object newState);
    }

    /// <summary>
    /// Executes for each input event and and can transform the event value into a different type.
    /// Use the 'Keep' version if you want to filter some events out.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    /// 
    public class ForEach<TState, TIn, TOut> : ObservableInputBase<TIn>, ISwappableGenericType, IFeedState
       where TState : class
    {
        //[HotSwap] //this resulted in alive subscriptions after hot swap in midi OutputDevice that could not be removed
        Subject<TOut> FSubject = new Subject<TOut>();
        bool FShouldReset;
        Func<TState> FCreator;
        Func<TState, TIn, Tuple<TState, TOut>> FUpdater;
        SynchronizationContext FSyncContext;
        internal TState FState;

        public IObservable<TOut> Update(IObservable<TIn> messages, bool reset, Func<TState> create, Func<TState, TIn, Tuple<TState, TOut>> update)
        {
            FShouldReset |= reset;
            FCreator = create;
            FUpdater = update;

            ManageObservable(messages);

            return FSubject;
        }     

        protected override void OnNext(TIn input)
        {
            var state = FState;
            if (state == null || FShouldReset)
            {
                FShouldReset = false;
                (state as IDisposable)?.Dispose();
                state = FCreator?.Invoke();

                // Capture synchronization context of the current thread
                FSyncContext = SynchronizationContext.Current;
            }

            var updater = FUpdater;
            if (updater != null && state != null)
            {
                var tuple = updater(state, input);
                state = tuple.Item1;

                if (!FSubject.IsDisposed)
                    FSubject.OnNext(tuple.Item2);
            }

            FState = state;
        }

        protected override void OnCompleted()
        {
            FSubject.OnCompleted();
            FSubject.Dispose();
            FSubject = new Subject<TOut>();
            base.OnCompleted();
        }

        protected override void ReleaseResources()
        {
            // Unscubscribe first before disposing the state. This is very important in case the OnNext call is on a different thread.
            base.ReleaseResources();
            FSubject.Dispose();

            ReleaseState(FState, FSyncContext);
        }

        object ISwappableGenericType.Swap(Type newType, Swapper swapObject)
        {
            var newState = swapObject(FState, newType.GetGenericArguments()[0]);
            var newForeach = (IFeedState)Activator.CreateInstance(newType);
            newForeach.SwapState(newState);
            this.Dispose();
            return newForeach;
        }

        void IFeedState.SwapState(object newState)
        {
            FState = (TState)newState;
        }
    }

    /// <summary>
    /// Executes for each input event and and can transform the event value into a different type.
    /// The second boolean output in the region decides whether to pass the current event on.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    public class ForEachKeep<TState, TIn, TOut> : ObservableInputBase<TIn>, ISwappableGenericType, IFeedState
       where TState : class
    {
        //[HotSwap] //this resulted in alive subscriptions after hot swap in midi OutputDevice that could not be removed
        Subject<TOut> FSubject = new Subject<TOut>();
        bool FShouldReset;
        Func<TState> FCreator;
        Func<TState, TIn, Tuple<TState, TOut, bool>> FUpdater;
        SynchronizationContext FSyncContext;
        TState FState;

        public IObservable<TOut> Update(IObservable<TIn> messages, bool reset, Func<TState> create, Func<TState, TIn, Tuple<TState, TOut, bool>> update)
        {
            FShouldReset |= reset;
            FCreator = create;
            FUpdater = update;

            ManageObservable(messages);       

            return FSubject;
        }

        protected override void OnNext(TIn input)
        {
            var state = FState;
            if (state == null || FShouldReset)
            {
                FShouldReset = false;
                (state as IDisposable)?.Dispose();
                state = FCreator?.Invoke();

                // Capture synchronization context of the current thread
                FSyncContext = SynchronizationContext.Current;
            }

            var updater = FUpdater;
            if (updater != null && state != null)
            {
                var tuple = updater(state, input);
                state = tuple.Item1;

                if (tuple.Item3 && !FSubject.IsDisposed)
                    FSubject.OnNext(tuple.Item2);
            }

            FState = state;
        }

        protected override void OnCompleted()
        {
            FSubject.OnCompleted();
            FSubject.Dispose();
            FSubject = new Subject<TOut>();
            base.OnCompleted();
        }

        protected override void ReleaseResources()
        {
            // Unscubscribe first before disposing the state. This is very important in case the OnNext call is on a different thread.
            base.ReleaseResources();
            FSubject.Dispose();

            ReleaseState(FState, FSyncContext);
        }

        object ISwappableGenericType.Swap(Type newType, Swapper swapObject)
        {
            var newState = swapObject(FState, newType.GetGenericArguments()[0]);
            var newForeach = (IFeedState)Activator.CreateInstance(newType);
            newForeach.SwapState(newState);
            this.Dispose();
            return newForeach;
        }

        void IFeedState.SwapState(object newState)
        {
            FState = (TState)newState;
        }
    }
}
