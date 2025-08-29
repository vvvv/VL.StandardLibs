#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using VL.Core;
using VL.Core.Import;
using VL.Core.PublicAPI;
using VL.Core.Utils;
using VL.Lib.Collections;
using VL.Lib.Control;
using VL.Lib.IO;

[assembly: ImportType(typeof(Synchronizer<,,>), Category = "Control.Experimental")]
[assembly: ImportType(typeof(SynchronizerInputIsKey<,,>), Name = "Synchronizer (InputIsKey)", Category = "Control.Experimental")]
[assembly: ImportType(typeof(SynchronizerVLObjectInput<,,>), Name = "Synchronizer (VLObjectInput)", Category = "Control.Experimental")]
[assembly: ImportType(typeof(ForEachKey), Name = "ForEach (Key)", Category = "Control.Experimental")]

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
                return Activator.CreateInstance(newType, [swapObject(S, newType.GenericTypeArguments[1])])!;
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

    [ProcessNode]
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

    [ProcessNode]
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

    [ProcessNode]
    public sealed class SynchronizerVLObjectInput<TState, TInput, TOutput> : IDisposable, ISwappableGenericType
        where TState : class
        where TInput : IVLObject
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



    [ProcessNode]
    [Region(SupportedBorderControlPoints = ControlPointType.Splicer, TypeConstraint = "IReadOnlyDictionary", TypeConstraintIsBaseType = true)]
    public class ForEachKey : IRegion<ForEachKey.IInlay>, IDisposable
    {
        public interface IInlay
        {
            void Update(object key);
        }

        private readonly Dictionary<object, PatchWithBorders> _patches = new();
        private Func<IInlay>? _inlayFactory;

        class PatchWithBorders : IDisposable
        {
            public PatchWithBorders(IInlay inlay)
            {
                Inlay = inlay;
            }
            public void Dispose()
            {
                if (Inlay is IDisposable disposable)
                    disposable.Dispose();
            }

            public IInlay Inlay;
            public bool MarkedForRemoval;
        }

        public void Dispose()
        {
            foreach (var p in _patches.Values)
                p.Dispose();
            _patches.Clear();
        }

        object _currentkey;

        public void Update(IEnumerable keys)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys)); 
                
            if (_inlayFactory == null)
                throw new InvalidOperationException("Patch Inlay Factory not set");

            foreach (var p in _patches)
                p.Value.MarkedForRemoval = true;

            foreach (var key in keys)
                if (_patches.TryGetValue(key, out var p))
                    p.MarkedForRemoval = false;

            foreach (var (k, p) in _patches)
            {
                if (p.MarkedForRemoval)
                {
                    // Yes, this is ok to remove while iterating see https://github.com/dotnet/runtime/issues/26314
                    _patches.Remove(k);
                    _currentkey = k;
                    p.Dispose();
                }
            }

            foreach (var (ospl, splicer) in _outputSplicers)
            {
                EnsureOutputSplicer(ospl);
                splicer.Dictionary.Clear();
            }

            foreach (var key in keys)
            {
                if (key == null)
                    throw new InvalidOperationException("Null keys are not supported");

                _currentkey = key;

                PatchWithBorders? patch;
                if (!_patches.TryGetValue(key, out patch))
                    _patches[key] = patch = new PatchWithBorders(_inlayFactory());

                if (patch == null)
                    throw new InvalidOperationException("Patch creation failed");

                patch.Inlay.Update(key);
            }
        }

        private OutputSplicer EnsureOutputSplicer(OutputDescription ospl)
        {
            if (!_outputSplicers.TryGetValue(ospl, out var outputSplicer))
                outputSplicer = _outputSplicers[ospl] = createDictionary(ospl.OuterType);
            return outputSplicer;
        }


        private OutputSplicer createDictionary(Type outerType)
        {
            if (outerType.IsGenericType && outerType.GetGenericTypeDefinition() == typeof(ImmutableDictionary<,>))
            {
                var genericArgs = outerType.GetGenericArguments();
                var method = typeof(ForEachKeyHelper)
                    .GetMethod(nameof(ForEachKeyHelper.CreateImmutableDictionary), BindingFlags.NonPublic | BindingFlags.Static);
                method = method?
                    .MakeGenericMethod(genericArgs);
                return new OutputSplicer(method?.Invoke(null, null) as IDictionary, true); // immutable dictionary builder created
            }

            if (outerType.IsSealed)
                return new OutputSplicer(Activator.CreateInstance(outerType) as IDictionary, false); // some dictionary created

            return new OutputSplicer(Activator.CreateInstance(findDictionaryType(outerType)) as IDictionary, false); // standard mutable dictionary created
        }

        Type findDictionaryType(Type outerType)
        {
            var dictType = outerType.YieldReturn().Concat(outerType.GetInterfaces()).FirstOrDefault(i => 
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));

            if (dictType == null)
                throw new Exception("Can't find dictionary");

            return typeof(Dictionary<,>).MakeGenericType(
                                dictType.GenericTypeArguments[0],
                                dictType.GenericTypeArguments[1]);
        }


        private readonly Dictionary<InputDescription, IDictionary> _inputSplicers = new();
        private readonly Dictionary<OutputDescription, OutputSplicer> _outputSplicers = new();
        private readonly Dictionary<InputDescription, object?> _links = new();

        class OutputSplicer
        {
            public IDictionary? Dictionary;
            public bool IsImmutable;

            public OutputSplicer(IDictionary? dictionary, bool isImmutable)
            {
                Dictionary = dictionary;
                IsImmutable = isImmutable;
            }
        }

        void IRegion<IInlay>.SetPatchInlayFactory(Func<IInlay> patchInlayFactory)
        {
            _inlayFactory = patchInlayFactory;
        }

        void IRegion<IInlay>.AcknowledgeInput(in InputDescription cp, object? outerValue)
        {
            if (cp.IsLink)
            {
                _links[cp] = outerValue;
                return;
            }

            if (outerValue is IDictionary dictionary)
                _inputSplicers[cp] = dictionary;
            else
                throw new InvalidOperationException("Input splicers must be of type IReadOnlyDictionary<TKey, TValue>");
        }

        void IRegion<IInlay>.RetrieveInput(in InputDescription cp, IInlay patchInstance, out object? innerValue)
        {
            if (cp.IsLink)
            {
                innerValue = _links[cp];
                return;
            }

            if (!_inputSplicers.TryGetValue(cp, out var dictionary))
                throw new Exception($"Input Splicer {cp} doesn't hold dictionary");

            innerValue = dictionary[_currentkey];
        }

        void IRegion<IInlay>.AcknowledgeOutput(in OutputDescription cp, IInlay patchInstance, object? innerValue)
        {
            var splicer = EnsureOutputSplicer(cp);

            splicer.Dictionary[_currentkey] = innerValue;
        }

        void IRegion<IInlay>.RetrieveOutput(in OutputDescription cp, out object? outerValue)
        {
            if (_outputSplicers.TryGetValue(cp, out var splicer))
            {
                var outerType = cp.OuterType;
                if (splicer.IsImmutable)
                    outerValue = (splicer.Dictionary as dynamic).ToImmutable();
                else
                    outerValue = splicer.Dictionary;
            }
            else
            {
                outerValue = null;
            }
        }

    }

    internal static class ForEachKeyHelper
    {
        internal static IDictionary? CreateImmutableDictionary<TKey, TValue>()
        {
            return ImmutableDictionary<TKey, TValue>.Empty.ToBuilder();
        }
    }
}
