using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Lib.Collections;

namespace VL.Lib.Control
{
    public class SynchronizerInputIsKey<TState, TInput, TOutput> : IDisposable
    {
        readonly ImmutableDictionary<TInput, TState> States;

        public SynchronizerInputIsKey(ImmutableDictionary<TInput, TState> states)
        {
            States = states ?? ImmutableDictionary<TInput, TState>.Empty;
        }

        public SynchronizerInputIsKey()
            : this(null)
        {
        }

        public SynchronizerInputIsKey<TState, TInput, TOutput> Update(
            IEnumerable<TInput> input, 
            Func<TInput, TState> create, 
            Func<TState, TInput, Tuple<TState, TOutput>> updator,
            out Spread<TOutput> outputs)
        {
            var oldStates = States.ToBuilder();
            var newStates = ImmutableDictionary<TInput, TState>.Empty.ToBuilder();
            var outputsBuilder = SpreadBuilder<TOutput>.Empty;
            foreach (var key in input)
                oldStates.Remove(key);
            foreach (var dying in oldStates.Values)
                (dying as IDisposable)?.Dispose();
            foreach (var i in input)
            {
                var key = i;
                if (!States.TryGetValue(key, out TState state))
                    state = create(i);
                TOutput output;
                (state, output) = updator(state, i);
                newStates[key] = state;
                outputsBuilder.Add(output);
            }

            outputs = outputsBuilder.ToSpread();
            return new SynchronizerInputIsKey<TState, TInput, TOutput>(newStates.ToImmutable());
        }

        public void Dispose()
        {
            foreach (var s in States.Values)
                if (s is IDisposable d)
                    d.Dispose();
        }
    }

    public class Synchronizer<TState, TInput, TOutput> : IDisposable
    {
        readonly ImmutableDictionary<object, TState> States;

        public Synchronizer(ImmutableDictionary<object, TState> states)
        {
            States = states ?? ImmutableDictionary<object, TState>.Empty;
        }

        public Synchronizer()
            : this(null)
        {
        }

        public Synchronizer<TState, TInput, TOutput> Update(
            IEnumerable<TInput> input, 
            Func<TInput, object> keySelector, 
            Func<TInput, TState> create, 
            Func<TState, TInput, Tuple<TState, TOutput>> updator,
            out Spread<TOutput> outputs)
        {
            var oldStates = States.ToBuilder();
            var newStates = ImmutableDictionary<object, TState>.Empty.ToBuilder();
            var outputsBuilder = SpreadBuilder<TOutput>.Empty;
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
                if (!States.TryGetValue(key, out TState state))
                    state = create(i);
                TOutput output;
                (state, output) = updator(state, i);
                newStates[key] = state;
                outputsBuilder.Add(output);
            }
            outputs = outputsBuilder.ToSpread();
            return new Synchronizer<TState, TInput, TOutput>(newStates.ToImmutable());
        }

        public void Dispose()
        {
            foreach (var s in States.Values)
                if (s is IDisposable d)
                    d.Dispose();
        }
    }

    public class SynchronizerVLObjectInput<TState, TInput, TOutput> : IDisposable, ISwappableGenericType
        where TInput: IVLObject
    {
        readonly ImmutableDictionary<object, TState> States;

        public SynchronizerVLObjectInput(ImmutableDictionary<object, TState> states)
        {
            States = states ?? ImmutableDictionary<object, TState>.Empty;
        }

        public SynchronizerVLObjectInput()
            : this(null)
        {
        }

        public SynchronizerVLObjectInput<TState, TInput, TOutput> Update(
            IEnumerable<TInput> input,
            Func<TInput, TState> create,
            Func<TState, TInput, Tuple<TState, TOutput>> updator,
            out Spread<TOutput> outputs)
        {
            var oldStates = States.ToBuilder();
            var newStates = ImmutableDictionary<object, TState>.Empty.ToBuilder();
            var outputsBuilder = SpreadBuilder<TOutput>.Empty;
            foreach (var i in input)
            {
                var key = i.Identity;
                oldStates.Remove(key);
            }
            foreach (var dying in oldStates.Values)
                (dying as IDisposable)?.Dispose();
            foreach (var i in input)
            {
                var key = i.Identity;
                if (!States.TryGetValue(key, out TState state))
                    state = create(i);
                TOutput output;
                (state, output) = updator(state, i);
                newStates[key] = state;
                outputsBuilder.Add(output);
            }

            outputs = outputsBuilder.ToSpread();
            return new SynchronizerVLObjectInput<TState, TInput, TOutput>(newStates.ToImmutable());
        }

        public void Dispose()
        {
            foreach (var s in States.Values)
                if (s is IDisposable d)
                    d.Dispose();
        }

        object ISwappableGenericType.Swap(Type newType, Swapper swapObject)
        {
            var newTState = newType.GetGenericArguments().Single(_ => _.Name == nameof(TState));
            var newdictType = typeof(ImmutableDictionary<,>).MakeGenericType(typeof(object), newTState);
            var newdict = swapObject(States, newdictType);
            return Activator.CreateInstance(newType, args: newdict);
        }
    }
}
