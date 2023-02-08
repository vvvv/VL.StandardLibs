/*
 * Implement this with plugins at a later stage. For now we need the delegate proxies for our observable wrappers only.
 */

using System;

namespace VL.Lib.Proxies
{
    public class ActionProxy<T1>
    {
        Action<T1> FDelegate;

        public void Update(Action<T1> @delegate)
        {
            FDelegate = @delegate;
        }

        public Action<T1> GetDelegate() => Invoke;

        void Invoke(T1 arg1) => FDelegate?.Invoke(arg1);
    }

    public class ActionProxy<T1, T2>
    {
        Action<T1, T2> FDelegate;

        public void Update(Action<T1, T2> @delegate)
        {
            FDelegate = @delegate;
        }

        public Action<T1, T2> GetDelegate() => Invoke;

        void Invoke(T1 arg1, T2 arg2) => FDelegate?.Invoke(arg1, arg2);
    }

    public class FuncProxy<TResult>
    {
        Func<TResult> FDelegate;

        public Func<TResult> Update(Func<TResult> @delegate)
        {
            FDelegate = @delegate;
            return Invoke;
        }

        TResult Invoke() => FDelegate.Invoke();
    }

    public class FuncProxy<T, TResult>
    {
        Func<T, TResult> FDelegate;

        public Func<T, TResult> Update(Func<T, TResult> @delegate)
        {
            FDelegate = @delegate;
            return Invoke;
        }

        TResult Invoke(T arg) => FDelegate.Invoke(arg);
    }

    public class FuncProxy<T1, T2, TResult>
    {
        Func<T1, T2, TResult> FDelegate;

        public Func<T1, T2, TResult> Update(Func<T1, T2, TResult> @delegate)
        {
            FDelegate = @delegate;
            return Invoke;
        }

        TResult Invoke(T1 arg1, T2 arg2) => FDelegate.Invoke(arg1, arg2);
    }

    public class FuncProxy<T1, T2, T3, TResult>
    {
        Func<T1, T2, T3, TResult> FDelegate;

        public Func<T1, T2, T3, TResult> Update(Func<T1, T2, T3, TResult> @delegate)
        {
            FDelegate = @delegate;
            return Invoke;
        }

        TResult Invoke(T1 arg1, T2 arg2, T3 arg3) => FDelegate.Invoke(arg1, arg2, arg3);
    }
}
