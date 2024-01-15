using System;
using System.Collections;
using System.Collections.Generic;

namespace VL.Core
{
    internal interface IHotswapSpecificNodes
    {
        static readonly IHotswapSpecificNodes Impl = AppHost.Global.Services.GetService<IHotswapSpecificNodes>();

        T HardCast<T>(object input);

        void CastAs<T>(object input, T @default, out T result, out bool success);

        void CastAsGeneric<TIn, TOut>(TIn input, TOut @default, out TOut result, out bool success);

        IEnumerable<TResult> OfType<TResult>(IEnumerable input);

        ISingleInstanceHelper<T> CreateSingleInstanceHelper<T>() where T : class;

        bool TryGetStateType(Type publicType, out Type stateType);

        T FromStateObject<T, TState>(TState state) where T : new();

        TState GetStateObject<T, TState>(T obj);

        event EventHandler ProgramInstantiated;
    }

    internal interface ISingleInstanceHelper<T>
        where T : class
    {
        T GetInstance(bool forceNewInstance, Func<T> producer, SingleInstanceBehaviorOnStop onStop);
    }
    public enum SingleInstanceBehaviorOnStop { Keep, Release, ReleaseAndDispose };
}
