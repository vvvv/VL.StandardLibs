#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using VL.Core;
using VL.Core.Utils;
using VL.Lib.Collections;

namespace VL.Lib.Control
{
    // Fast version of Synchronizer that uses a mutable dictionary
    internal sealed class MutableSynchronizer<TKey, TState, TInput, TOutput> : ISynchronizer<TKey, TState, TInput, TOutput>
        where TState : class
        where TKey : notnull
    {
        private sealed class State : ISwappableGenericType, IDisposable
        { 
            public State(TState s) => S = s;

            public TState S; 
            public bool MarkedForRemoval;

            public object Swap(Type newType, Swapper swapObject)
            {
                return Activator.CreateInstance(newType, [ swapObject(S, newType.GenericTypeArguments[1])])!;
            }

            public void Dispose()
            {
                if (S is IDisposable disposable) 
                    disposable.Dispose();
            }
        }

        private readonly Dictionary<TKey, State> states;
        private Spread<TOutput> outputs;

        private MutableSynchronizer(Dictionary<TKey, State> states, Spread<TOutput> outputs)
        {
            this.states = states;
            this.outputs = outputs;
        }

        public MutableSynchronizer()
            : this(new(), Spread<TOutput>.Empty)
        {
        }

        public ISynchronizer<TKey, TState, TInput, TOutput> Update(
            IEnumerable<TInput> input,
            Func<TInput, TKey> keySelector,
            Func<TInput, TState> create,
            Func<TState, TInput, Tuple<TState, TOutput>> updator,
            out Spread<TOutput> outputs)
        {
            var outputsBuilder = CollectionBuilders.GetBuilder(this.outputs, 0);

            foreach (var s in states)
                s.Value.MarkedForRemoval = true;

            // https://github.com/devvvvs/vvvv/issues/6942
            // Keys are not necessarily unique, so we need to keep the input in a list to ensure a correct output spread
            using var preparedInput = Pooled.GetList<(TKey key, TInput input)>();
            foreach (var i in input)
            {
                var key = keySelector(i);
                preparedInput.Value.Add((key, i));
                if (states.TryGetValue(key, out var state))
                    state.MarkedForRemoval = false;
            }

            foreach (var (k, s) in states)
            {
                if (s.MarkedForRemoval)
                {
                    // Yes, this is ok to remove while iterating see https://github.com/dotnet/runtime/issues/26314
                    states.Remove(k);
                    s.Dispose();
                }
            }

            foreach (var (key, i) in preparedInput.Value)
            {
                if (!states.TryGetValue(key, out var state))
                    state = states[key] = new(create(i));

                var (newState, output) = updator(state.S, i);
                state.S = newState;

                outputsBuilder.Add(output);
            }

            outputs = this.outputs = outputsBuilder.Commit();
            return this;
        }

        public void Dispose()
        {
            foreach (var s in states.Values)
                s.Dispose();
            states.Clear();
        }

        object? ISwappableGenericType.Swap(Type newType, Swapper swapObject)
        {
            var args = newType.GetGenericArguments();
            var newTKey = args[0];
            var newState = typeof(State).GetGenericTypeDefinition().MakeGenericType(args);
            var newTOutput = args[3];
            var newdictType = typeof(Dictionary<,>).MakeGenericType(newTKey, newState);
            var newdict = swapObject(states, newdictType);
            var newSpreadType = typeof(Spread<>).MakeGenericType(newTOutput);
            var newspread = swapObject(outputs, newSpreadType);
            return Activator.CreateInstance(newType, binder: null, bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic, args: [newdict, newspread], culture: null);
        }
    }

    // Slow version using an immutable dictionary to ensure compatibility with potential patches making use of it in records
    internal sealed class ImmutableSynchronizer<TKey, TState, TInput, TOutput> : ISynchronizer<TKey, TState, TInput, TOutput>
        where TState : class
        where TKey : notnull
    {
        private readonly ImmutableDictionary<TKey, TState> states;
        private readonly Spread<TOutput> outputs;

        private ImmutableSynchronizer(ImmutableDictionary<TKey, TState> states, Spread<TOutput> outputs)
        {
            this.states = states;
            this.outputs = outputs;
        }

        public ImmutableSynchronizer()
            : this(ImmutableDictionary<TKey, TState>.Empty, Spread<TOutput>.Empty)
        {
        }

        public ISynchronizer<TKey, TState, TInput, TOutput> Update(
            IEnumerable<TInput> input,
            Func<TInput, TKey> keySelector,
            Func<TInput, TState> create,
            Func<TState, TInput, Tuple<TState, TOutput>> updator,
            out Spread<TOutput> outputs)
        {
            var oldStates = this.states.ToBuilder();
            var newStatesBuilder = CollectionBuilders.GetBuilder(this.states, 0);

            var outputsBuilder = CollectionBuilders.GetBuilder(this.outputs, 0);
            foreach (var i in input)
            {
                var key = keySelector(i);
                oldStates.Remove(key);
            }
            foreach (var dying in oldStates.Values)
                (dying as IDisposable)?.Dispose();
            foreach (var i in input)
            {
                var key = keySelector(i);
                if (!this.states.TryGetValue(key, out var state))
                    state = create(i);
                TOutput output;
                (state, output) = updator(state, i);
                newStatesBuilder.Add(key, state);
                outputsBuilder.Add(output);
            }

            outputs = outputsBuilder.Commit();
            var states = newStatesBuilder.Commit();
            if (outputs != this.outputs || states != this.states)
                return new ImmutableSynchronizer<TKey, TState, TInput, TOutput>(states, outputs);
            return this;
        }

        public void Dispose()
        {
            foreach (var s in states.Values)
                if (s is IDisposable d)
                    d.Dispose();
        }

        object? ISwappableGenericType.Swap(Type newType, Swapper swapObject)
        {
            var args = newType.GetGenericArguments();
            var newTKey = args[0];
            var newTState = args[1];
            var newTOutput = args[3];
            var newdictType = typeof(ImmutableDictionary<,>).MakeGenericType(newTKey, newTState);
            var newdict = swapObject(states, newdictType);
            var newSpreadType = typeof(Spread<>).MakeGenericType(newTOutput);
            var newspread = swapObject(outputs, newSpreadType);
            return Activator.CreateInstance(newType, binder: null, bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic, args: [newdict, newspread], culture: null);
        }
    }

    internal interface ISynchronizer<TKey, TState, TInput, TOutput> : IDisposable, ISwappableGenericType
        where TKey : notnull
    {
        ISynchronizer<TKey, TState, TInput, TOutput> Update(
            IEnumerable<TInput> input,
            Func<TInput, TKey> keySelector,
            Func<TInput, TState> create,
            Func<TState, TInput, Tuple<TState, TOutput>> updator,
            out Spread<TOutput> outputs);
    }

    public sealed class SynchronizerInputIsKey<TState, TInput, TOutput> : IDisposable, ISwappableGenericType
        where TState : class
        where TInput : notnull
    {
        private readonly ISynchronizer<TInput, TState, TInput, TOutput> impl;

        private SynchronizerInputIsKey(ISynchronizer<TInput, TState, TInput, TOutput> impl)
        {
            this.impl = impl;
        }

        public SynchronizerInputIsKey(NodeContext nodeContext)
            : this(nodeContext.IsImmutable ? new ImmutableSynchronizer<TInput, TState, TInput, TOutput>() : new MutableSynchronizer<TInput, TState, TInput, TOutput>())
        {
        }

        public SynchronizerInputIsKey<TState, TInput, TOutput> Update(
            IEnumerable<TInput> input, 
            Func<TInput, TState> create, 
            Func<TState, TInput, Tuple<TState, TOutput>> updator,
            out Spread<TOutput> outputs)
        {
            var impl = this.impl.Update(input, i => i, create, updator, out outputs);
            if (impl != this.impl)
                return new(impl);
            return this;
        }

        public void Dispose() => impl.Dispose();

        object? ISwappableGenericType.Swap(Type newType, Swapper swapObject)
        {
            var args = newType.GetGenericArguments();
            var newTState = args[0];
            var newTInput = args[1];
            var newTOutput = args[2];
            var newImplType = impl.GetType().GetGenericTypeDefinition().MakeGenericType(newTInput, newTState, newTInput, newTOutput);
            var newimpl = swapObject(this.impl, newImplType);
            return Activator.CreateInstance(newType, binder: null, bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic, args: [newimpl], culture: null);
        }
    }

    public sealed class Synchronizer<TState, TInput, TOutput> : IDisposable, ISwappableGenericType
        where TState : class
        where TInput : notnull
    {
        private readonly ISynchronizer<object, TState, TInput, TOutput> impl;

        public Synchronizer(NodeContext nodeContext)
            : this(nodeContext.IsImmutable ? new ImmutableSynchronizer<object, TState, TInput, TOutput>() : new MutableSynchronizer<object, TState, TInput, TOutput>()) 
        {
        }

        private Synchronizer(ISynchronizer<object, TState, TInput, TOutput> impl) => this.impl = impl;

        public Synchronizer<TState, TInput, TOutput> Update(
            IEnumerable<TInput> input, 
            Func<TInput, object> keySelector, 
            Func<TInput, TState> create, 
            Func<TState, TInput, Tuple<TState, TOutput>> updator,
            out Spread<TOutput> outputs)
        {
            var impl = this.impl.Update(input, keySelector, create, updator, out outputs);
            if (impl != this.impl)
                return new(impl);
            return this;
        }

        public void Dispose() => impl.Dispose();

        object? ISwappableGenericType.Swap(Type newType, Swapper swapObject)
        {
            var args = newType.GetGenericArguments();
            var newImplType = impl.GetType().GetGenericTypeDefinition().MakeGenericType(typeof(object), args[0], args[1], args[2]);
            var newimpl = swapObject(this.impl, newImplType);
            return Activator.CreateInstance(newType, binder: null, bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic, args: [newimpl], culture: null);
        }
    }

    public sealed class SynchronizerVLObjectInput<TState, TInput, TOutput> : IDisposable, ISwappableGenericType
        where TState : class
        where TInput: IVLObject
    {
        private readonly ISynchronizer<uint, TState, TInput, TOutput> impl;

        private SynchronizerVLObjectInput(ISynchronizer<uint, TState, TInput, TOutput> impl) => this.impl = impl;

        public SynchronizerVLObjectInput(NodeContext nodeContext) 
            : this(nodeContext.IsImmutable ? new ImmutableSynchronizer<uint, TState, TInput, TOutput>() : new MutableSynchronizer<uint, TState, TInput, TOutput>())
        { 
        }

        public SynchronizerVLObjectInput<TState, TInput, TOutput> Update(
            IEnumerable<TInput> input,
            Func<TInput, TState> create,
            Func<TState, TInput, Tuple<TState, TOutput>> updator,
            out Spread<TOutput> outputs)
        {
            var impl = this.impl.Update(input, i => i.Identity, create, updator, out outputs);
            if (impl != this.impl)
                return new(impl);
            return this;
        }

        public void Dispose() => impl.Dispose();

        object? ISwappableGenericType.Swap(Type newType, Swapper swapObject)
        {
            var args = newType.GetGenericArguments();
            var newImplType = impl.GetType().GetGenericTypeDefinition().MakeGenericType(typeof(uint), args[0], args[1], args[2]);
            var newimpl = swapObject(this.impl, newImplType);
            return Activator.CreateInstance(newType, binder: null, bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic, args: [newimpl], culture: null);
        }
    }
}
