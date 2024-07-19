#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Core.Utils;
using VL.Lib.Collections;

namespace VL.Lib.Control
{
    internal sealed class Synchronizer<TKey, TState, TInput, TOutput> : IDisposable, ISwappableGenericType
        where TKey : notnull
    {
        private readonly ImmutableDictionary<TKey, TState> states;
        private readonly Spread<TOutput> outputs;

        private Synchronizer(ImmutableDictionary<TKey, TState> states, Spread<TOutput> outputs)
        {
            this.states = states;
            this.outputs = outputs;
        }

        public Synchronizer()
            : this(ImmutableDictionary<TKey, TState>.Empty, Spread<TOutput>.Empty)
        {
        }

        public Synchronizer<TKey, TState, TInput, TOutput> Update(
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
                return new Synchronizer<TKey, TState, TInput, TOutput>(states, outputs);
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

    public sealed class SynchronizerInputIsKey<TState, TInput, TOutput> : IDisposable, ISwappableGenericType
        where TInput : notnull
    {
        private readonly Synchronizer<TInput, TState, TInput, TOutput> impl;

        private SynchronizerInputIsKey(Synchronizer<TInput, TState, TInput, TOutput> impl)
        {
            this.impl = impl;
        }

        public SynchronizerInputIsKey()
            : this(new())
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
            var newImplType = typeof(Synchronizer<,,,>).MakeGenericType(newTInput, newTState, newTInput, newTOutput);
            var newimpl = swapObject(this.impl, newImplType);
            return Activator.CreateInstance(newType, binder: null, bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic, args: [newimpl], culture: null);
        }
    }

    public sealed class Synchronizer<TState, TInput, TOutput> : IDisposable, ISwappableGenericType
        where TInput : notnull
    {
        private readonly Synchronizer<object, TState, TInput, TOutput> impl;

        public Synchronizer() : this(new()) { }

        private Synchronizer(Synchronizer<object, TState, TInput, TOutput> impl) => this.impl = impl;

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
            var newImplType = typeof(Synchronizer<,,,>).MakeGenericType(typeof(object), args[0], args[1], args[2]);
            var newimpl = swapObject(this.impl, newImplType);
            return Activator.CreateInstance(newType, binder: null, bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic, args: [newimpl], culture: null);
        }
    }

    public sealed class SynchronizerVLObjectInput<TState, TInput, TOutput> : IDisposable, ISwappableGenericType
        where TInput: IVLObject
    {
        private readonly Synchronizer<uint, TState, TInput, TOutput> impl;

        private SynchronizerVLObjectInput(Synchronizer<uint, TState, TInput, TOutput> impl) => this.impl = impl;

        public SynchronizerVLObjectInput() : this(new()) { }

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
            var newImplType = typeof(Synchronizer<,,,>).MakeGenericType(typeof(uint), args[0], args[1], args[2]);
            var newimpl = swapObject(this.impl, newImplType);
            return Activator.CreateInstance(newType, binder: null, bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic, args: [newimpl], culture: null);
        }
    }
}
