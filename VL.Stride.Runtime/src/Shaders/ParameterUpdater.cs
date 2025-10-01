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
        private readonly ParameterCollection parameters;

        // In case we end up in a shader graph multiple parameter collections could pop up (one for every effect) we need to keep track of
        private Dictionary<(ParameterCollection, ColorSpace, TKey), RefCountDisposable> trackedCollections;

        private TValue value;
        private TKey key;

        public ParameterUpdater(ParameterCollection parameters = default, TKey key = default)
        {
            this.parameters = parameters;
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

                    if (parameters != null)
                    {
                        Upload(parameters, key, colorSpace: default, ref value);
                    }

                    if (trackedCollections != null)
                    {
                        foreach (var (parameters, colorSpace, key) in trackedCollections.Keys)
                            Upload(parameters, key, colorSpace, ref value);
                    }
                }
            }
        }

        public ImmutableArray<ParameterCollection> GetTrackedCollections()
        {
            if (trackedCollections is null)
                return ImmutableArray<ParameterCollection>.Empty;

            var result = ImmutableArray.CreateBuilder<ParameterCollection>(trackedCollections.Count);
            foreach (var (parameters, _, _) in trackedCollections.Keys)
                result.Add(parameters);
            return result.ToImmutable();
        }

        public void Track(ShaderGeneratorContext context)
        {
            Track(context, key);
        }

        public void Track(ShaderGeneratorContext context, TKey key)
        {
            if (context.TryGetSubscriptions(out var s))
                s.Add(Subscribe(context.Parameters, context.ColorSpace, key));
        }

        public IDisposable Subscribe(ParameterCollection parameters, ColorSpace colorSpace, TKey key)
        {
            var x = (parameters, colorSpace, key);

            var trackedCollections = this.trackedCollections ??= new();
            if (trackedCollections.TryGetValue(x, out var disposable))
                return disposable.GetDisposable();

            disposable = new RefCountDisposable(Disposable.Create(() => trackedCollections.Remove(x)));
            trackedCollections.Add(x, disposable);
            Upload(parameters, key, colorSpace, ref value);
            return disposable;
        }

        protected abstract void Upload(ParameterCollection parameters, TKey key, ColorSpace colorSpace, ref TValue value);
    }

    public sealed class ValueParameterUpdater<T> : ParameterUpdater<T, ValueParameterKey<T>>
        where T : struct
    {
        private bool convertToDeviceColorSpace;

        public ValueParameterUpdater(ParameterCollection parameters = null, ValueParameterKey<T> key = null, bool convertToDeviceColorSpace = false) : base(parameters, key)
        {
            this.convertToDeviceColorSpace = convertToDeviceColorSpace;
        }

        protected override void Upload(ParameterCollection parameters, ValueParameterKey<T> key, ColorSpace colorSpace, ref T value)
        {
            if (convertToDeviceColorSpace && typeof(T) == typeof(Color4))
            {
                // We do the same as what the ColorIn node does in its default settings
                var color = Unsafe.BitCast<T, Color4>(value);
                var deviceColor = Color4.PremultiplyAlpha(color.ToColorSpace(colorSpace));
                parameters.Set(key, Unsafe.BitCast<Color4, T>(deviceColor));
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
        public ArrayParameterUpdater(ParameterCollection parameters = null, ValueParameterKey<T> key = null) : base(parameters, key)
        {

        }

        protected override void Upload(ParameterCollection parameters, ValueParameterKey<T> key, ColorSpace colorSpace, ref T[] value)
        {
            if (value.Length > 0)
                parameters.Set(key, value);
        }
    }

    public sealed class ObjectParameterUpdater<T> : ParameterUpdater<T, ObjectParameterKey<T>>
        where T : class
    {
        public ObjectParameterUpdater(ParameterCollection parameters = null, ObjectParameterKey<T> key = null) : base(parameters, key)
        {

        }

        protected override void Upload(ParameterCollection parameters, ObjectParameterKey<T> key, ColorSpace colorSpace, ref T value)
        {
            parameters.Set(key, value);
        }
    }

    public sealed class PermutationParameterUpdater<T> : ParameterUpdater<T, PermutationParameterKey<T>>
    {
        public PermutationParameterUpdater(ParameterCollection parameters = null, PermutationParameterKey<T> key = null) : base(parameters, key)
        {

        }

        protected override void Upload(ParameterCollection parameters, PermutationParameterKey<T> key, ColorSpace colorSpace, ref T value)
        {
            parameters.Set(key, value);
        }
    }
}
