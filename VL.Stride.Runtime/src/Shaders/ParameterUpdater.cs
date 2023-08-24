using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;

namespace VL.Stride.Shaders.ShaderFX
{
    /// <summary>
    /// Helper class to easily track parameter collections and update one of its parameters.
    /// </summary>
    /// <typeparam name="TValue">The type of the parameter value</typeparam>
    /// <typeparam name="TKey">The type of the parameter key</typeparam>
    public abstract class ParameterUpdater<TValue, TKey>
        where TKey : ParameterKey
    {
        private static readonly EqualityComparer<TValue> comparer = EqualityComparer<TValue>.Default;

        // In most of the cases the parameter collection is known from the start and no other will come into play (pin and effect are in the same node)
        private readonly ShaderGeneratorContext singleContext;

        // In case we end up in a shader graph multiple parameter collections could pop up (one for every effect) we need to keep track of
        private Dictionary<(ShaderGeneratorContext, TKey), (RefCountDisposable subscription, ParameterCollection collection)> trackedContexts;

        private TValue value;
        private TKey key;

        public ParameterUpdater(ShaderGeneratorContext context = default, TKey key = default)
        {
            this.singleContext = context;
            this.key = key;
        }

        public TValue Value
        {
            get => value;
            set
            {
                if (!comparer.Equals(value, Value))
                {
                    this.value = value;

                    if (singleContext != null)
                    {
                        Upload(singleContext, key, ref value);
                    }

                    if (trackedContexts != null)
                    {
                        foreach (var ((context, key), (_, parameters)) in trackedContexts)
                        {
                            // Little bit sad, but it Stride sets the Parameters property to null after generating its materials. Keep track of them manually.
                            var currentParameters = context.Parameters;
                            context.Parameters = parameters;
                            Upload(context, key, ref value);
                            context.Parameters = currentParameters;
                        }
                    }
                }
            }
        }

        public ImmutableArray<ShaderGeneratorContext> GetTrackedConexts()
        {
            if (trackedContexts is null)
                return ImmutableArray<ShaderGeneratorContext>.Empty;

            var result = ImmutableArray.CreateBuilder<ShaderGeneratorContext>(trackedContexts.Count);
            foreach (var (context, _) in trackedContexts.Keys)
                result.Add(context);
            return result.ToImmutable();
        }

        public void Track(ShaderGeneratorContext context)
        {
            Track(context, key);
        }

        public void Track(ShaderGeneratorContext context, TKey key)
        {
            if (context.TryGetSubscriptions(out var s))
                s.Add(Subscribe(context, key));
        }

        public IDisposable Subscribe(ShaderGeneratorContext context, TKey key)
        {
            var trackingKey = (context, key);

            var trackedCollections = this.trackedContexts ??= new();
            if (trackedCollections.TryGetValue(trackingKey, out var x))
                return x.subscription.GetDisposable();

            x = (new RefCountDisposable(Disposable.Create(() => trackedCollections.Remove(trackingKey))), context.Parameters);
            trackedCollections.Add(trackingKey, x);
            Upload(context, key, ref value);
            return x.subscription;
        }

        protected abstract void Upload(ShaderGeneratorContext context, TKey key, ref TValue value);
    }

    public sealed class ValueParameterUpdater<T> : ParameterUpdater<T, ValueParameterKey<T>>
        where T : struct
    {
        // ABI compatibility
        [Obsolete("Will be removed in 6.0 release", error: true)]
        public ValueParameterUpdater(ParameterCollection context = null, ValueParameterKey<T> key = null) : base(null, key)
        {
        }

        public ValueParameterUpdater(ShaderGeneratorContext context, ValueParameterKey<T> key = null) : base(context, key)
        {
        }

        protected override void Upload(ShaderGeneratorContext context, ValueParameterKey<T> key, ref T value)
        {
            var parameters = context.Parameters;
            if (typeof(T) == typeof(Color4))
            {
                var deviceColor = Unsafe.As<T, Color4>(ref value).ToColorSpace(context.ColorSpace);
                var colorKey = Unsafe.As<ValueParameterKey<T>, ValueParameterKey<Color4>>(ref key);
                parameters.Set(colorKey, ref deviceColor);
            }
            else
            {
                parameters.Set(key, ref value);
            }
        }
    }

    public sealed class ArrayParameterUpdater<T> : ParameterUpdater<T[], ValueParameterKey<T>>
        where T : struct
    {
        // ABI compatibility
        [Obsolete("Will be removed in 6.0 release", error: true)]
        public ArrayParameterUpdater(ParameterCollection context = null, ValueParameterKey<T> key = null) : base(null, key)
        {
        }

        public ArrayParameterUpdater(ShaderGeneratorContext context = null, ValueParameterKey<T> key = null) : base(context, key)
        {

        }

        protected override void Upload(ShaderGeneratorContext context, ValueParameterKey<T> key, ref T[] value)
        {
            var parameters = context.Parameters;
            if (value.Length > 0)
                parameters.Set(key, value);
        }
    }

    public sealed class ObjectParameterUpdater<T> : ParameterUpdater<T, ObjectParameterKey<T>>
        where T : class
    {
        // ABI compatibility
        [Obsolete("Will be removed in 6.0 release", error: true)]
        public ObjectParameterUpdater(ParameterCollection context = null, ObjectParameterKey<T> key = null) : base(null, key)
        {
        }

        public ObjectParameterUpdater(ShaderGeneratorContext context = null, ObjectParameterKey<T> key = null) : base(context, key)
        {

        }

        protected override void Upload(ShaderGeneratorContext context, ObjectParameterKey<T> key, ref T value)
        {
            var parameters = context.Parameters;
            parameters.Set(key, value);
        }
    }

    public sealed class PermutationParameterUpdater<T> : ParameterUpdater<T, PermutationParameterKey<T>>
    {
        // ABI compatibility
        [Obsolete("Will be removed in 6.0 release", error: true)]
        public PermutationParameterUpdater(ParameterCollection context = null, PermutationParameterKey<T> key = null) : base(null, key)
        {
        }

        public PermutationParameterUpdater(ShaderGeneratorContext context = null, PermutationParameterKey<T> key = null) : base(context, key)
        {

        }

        protected override void Upload(ShaderGeneratorContext context, PermutationParameterKey<T> key, ref T value)
        {
            var parameters = context.Parameters;
            parameters.Set(key, value);
        }
    }
}
