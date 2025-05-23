using System;
using System.Collections.Generic;
using System.Reflection;
using VL.Core;
using Stride.Rendering;
using Stride.Core.Mathematics;
using Stride.Graphics;
using System.Runtime.CompilerServices;
using Stride.Rendering.Materials;
using VL.Stride.Shaders.ShaderFX;
using VL.Stride.Shaders.ShaderFX.Control;
using Stride.Shaders;

namespace VL.Stride.Rendering
{
    abstract class EffectPinDescription : IVLPinDescription, IInfo, IVLPinDescriptionWithVisibility
    {
        public abstract string Name { get; }
        public abstract Type Type { get; }
        public abstract object DefaultValueBoxed { get; }

        public string Summary { get; set; }
        public string Remarks { get; set; }

        public bool IsVisible { get; set; } = true;

        object IVLPinDescription.DefaultValue
        {
            get
            {
                // The Gpu<T> code path seems to use SetVar<T> here - we can't really deal with this in target code generation.
                // Therefor explicitly return null here, so the target code will not try to insert the types default through the monadic builder interface.
                if (DefaultValueBoxed is IComputeNode)
                    return null;

                return DefaultValueBoxed;
            }
        }

        public abstract IVLPin CreatePin(GraphicsDevice graphicsDevice, ParameterCollection parameters);
    }

    class PinDescription<T> : EffectPinDescription
    {
        public PinDescription(string name, T defaultValue = default(T))
        {
            Name = name;
            DefaultValue = defaultValue;
        }

        public override string Name { get; }
        public override Type Type => typeof(T);
        public override object DefaultValueBoxed => DefaultValue;
        public T DefaultValue { get; }

        public override IVLPin CreatePin(GraphicsDevice graphicsDevice, ParameterCollection parameters) => new Pin<T>(Name, DefaultValue);
    }

    /// <summary>
    /// Currently used for texture input pins of TextureFX nodes that need access to the original ParameterKey of the shader.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="VL.Stride.Rendering.EffectPinDescription" />
    class ParameterKeyPinDescription<T> : PinDescription<T>
    {
        public ParameterKeyPinDescription(string name, ParameterKey<T> key, T defaultValue = default(T))
            : base(name, defaultValue)
        {
            Key = key;
        }

        public ParameterKey<T> Key { get; }
    }

    class ParameterPinDescription : EffectPinDescription
    {
        public readonly ParameterKey Key;
        public readonly int Count;
        public readonly bool IsPermutationKey;
        public readonly object RuntimeDefaultValue;
        private readonly string? nodeInstanceId;
        private readonly bool isStage;
        private readonly ParameterKey internalKey;

        // Accept isStage as a parameter, determined by the caller from shader metadata/variable attributes
        public ParameterPinDescription(HashSet<string> usedNames, ParameterKey key, int count = 1, object compilationDefaultValue = null, bool isPermutationKey = false, string name = null, Type typeInPatch = null, object runtimeDefaultValue = null, string? nodeInstanceId = null, bool isStage = false)
        {
            Key = key;
            IsPermutationKey = isPermutationKey;
            Count = count;
            this.nodeInstanceId = nodeInstanceId;
            this.isStage = isStage;
            // User-facing name should not include nodeInstanceId
            Name = name ?? key.GetPinName(usedNames);
            // Internal shader key: unique per node unless 'stage'
            if (!isStage && nodeInstanceId != null)
                internalKey = key.WithUniqueInstance(nodeInstanceId);
            else
                internalKey = key;
            var elementType = typeInPatch ?? key.PropertyType;
            compilationDefaultValue = compilationDefaultValue ?? key.DefaultValueMetadata?.GetDefaultValue();
            // TODO: This should be fixed in Stride
            if (key.PropertyType == typeof(Matrix))
                compilationDefaultValue = Matrix.Identity;
            if (count > 1)
            {
                Type = elementType.MakeArrayType();
                var arr = Array.CreateInstance(elementType, count);
                for (int i = 0; i < arr.Length; i++)
                    arr.SetValue(compilationDefaultValue, i);
                DefaultValueBoxed = arr;
            }
            else
            {
                Type = elementType;
                DefaultValueBoxed = compilationDefaultValue;
            }
            RuntimeDefaultValue = runtimeDefaultValue ?? DefaultValueBoxed;
        }

        public override string Name { get; }
        public override Type Type { get; }
        public override object DefaultValueBoxed { get; }
        public override IVLPin CreatePin(GraphicsDevice graphicsDevice, ParameterCollection parameters)
        {
            // Use the internalKey for shader parameter assignment
            return EffectPins.CreatePin(graphicsDevice, parameters, internalKey, Count, IsPermutationKey, RuntimeDefaultValue, Type);
        }

        public override string ToString()
        {
            return "PinDesc: " + Name ?? base.ToString();
        }
    }

    static class EffectPins
    {
        public static IVLPin CreatePin(GraphicsDevice graphicsDevice, ParameterCollection parameters, ParameterKey key, int count, bool isPermutationKey, object value, Type typeInPatch)
        {
            if (key is ValueParameterKey<Color4> colorKey)
                return new ColorParameterPin(parameters, colorKey, graphicsDevice.ColorSpace, (Color4)value);

            var argument = key.GetType().GetGenericArguments()[0];

            if (typeInPatch.IsEnum)
            {
                var createPinMethod = typeof(EffectPins).GetMethod(nameof(CreateEnumPin), BindingFlags.Static | BindingFlags.Public);
                return createPinMethod.MakeGenericMethod(argument, typeInPatch).Invoke(null, new object[] { parameters, key, value }) as IVLPin;
            }

            if (argument == typeof(ShaderSource))
            {
                if (typeInPatch.IsGenericType && typeInPatch.GetGenericTypeDefinition() == typeof(SetVar<>))
                {
                    var typeParam = typeInPatch.GetGenericArguments()[0];
                    var createPinMethod = typeof(EffectPins).GetMethod(nameof(CreateGPUValueSinkPin), BindingFlags.Static | BindingFlags.Public);
                    return createPinMethod.MakeGenericMethod(typeParam).Invoke(null, new[] { parameters, key, value }) as IVLPin;
                }
                else
                {
                    var createPinMethod = typeof(EffectPins).GetMethod(nameof(CreateShaderFXPin), BindingFlags.Static | BindingFlags.Public);
                    return createPinMethod.MakeGenericMethod(typeof(IComputeNode)).Invoke(null, new object[] { parameters, key, value }) as IVLPin;
                }
            }

            if (isPermutationKey)
            {
                var createPinMethod = typeof(EffectPins).GetMethod(nameof(CreatePermutationPin), BindingFlags.Static | BindingFlags.Public);
                return createPinMethod.MakeGenericMethod(argument).Invoke(null, new object[] { parameters, key, value }) as IVLPin;
            }
            else if (argument.IsValueType)
            {
                if (count > 1)
                {
                    var createPinMethod = typeof(EffectPins).GetMethod(nameof(CreateArrayPin), BindingFlags.Static | BindingFlags.Public);
                    return createPinMethod.MakeGenericMethod(argument).Invoke(null, new object[] { parameters, key, value }) as IVLPin;
                }
                else
                {
                    var createPinMethod = typeof(EffectPins).GetMethod(nameof(CreateValuePin), BindingFlags.Static | BindingFlags.Public);
                    return createPinMethod.MakeGenericMethod(argument).Invoke(null, new object[] { parameters, key, value }) as IVLPin;
                }
            }
            else
            {
                var createPinMethod = typeof(EffectPins).GetMethod(nameof(CreateResourcePin), BindingFlags.Static | BindingFlags.Public);
                return createPinMethod.MakeGenericMethod(argument).Invoke(null, new object[] { parameters, key }) as IVLPin;
            }
        }

        public static IVLPin CreatePermutationPin<T>(ParameterCollection parameters, PermutationParameterKey<T> key, T value)
        {
            return new PermutationParameterPin<T>(parameters, key, value);
        }

        public static IVLPin CreateEnumPin<T, TEnum>(ParameterCollection parameters, ValueParameterKey<T> key, TEnum value) where T : unmanaged where TEnum : unmanaged
        {
            return new EnumParameterPin<T, TEnum>(parameters, key, value);
        }

        public static IVLPin CreateShaderFXPin<T>(ParameterCollection parameters, PermutationParameterKey<ShaderSource> key, T value) where T : class, IComputeNode
        {
            return new ShaderFXPin<T>(key, value);
        }

        public static IVLPin CreateGPUValueSinkPin<T>(ParameterCollection parameters, PermutationParameterKey<ShaderSource> key, SetVar<T> value)
        {
            return new GPUValueSinkPin<T>(key, value);
        }

        public static IVLPin CreateValuePin<T>(ParameterCollection parameters, ValueParameterKey<T> key, T value) where T : struct
        {
            return new ValueParameterPin<T>(parameters, key, value);
        }

        public static IVLPin CreateArrayPin<T>(ParameterCollection parameters, ValueParameterKey<T> key, T[] value) where T : struct
        {
            return new ArrayValueParameterPin<T>(parameters, key, value);
        }

        public static IVLPin CreateResourcePin<T>(ParameterCollection parameters, ObjectParameterKey<T> key) where T : class
        {
            return new ResourceParameterPin<T>(parameters, key);
        }
    }

    abstract class ParameterPin
    {
        internal abstract void SubscribeTo(ShaderGeneratorContext c);
    }

    abstract class ParameterPin<T, TKey> : ParameterPin, IVLPin<T>
        where TKey : ParameterKey
    {
        private readonly ParameterUpdater<T, TKey> updater;

        protected ParameterPin(ParameterUpdater<T, TKey> updater, T value)
        {
            this.updater = updater;
            this.updater.Value = value;
        }

        public T Value 
        { 
            get => updater.Value;
            set => updater.Value = value;
        }

        object IVLPin.Value 
        { 
            get => Value; 
            set => Value = (T)value; 
        }

        internal override sealed void SubscribeTo(ShaderGeneratorContext c)
        {
            updater.Track(c);
        }
    }

    sealed class PermutationParameterPin<T> : ParameterPin<T, PermutationParameterKey<T>>
    {
        public PermutationParameterPin(ParameterCollection parameters, PermutationParameterKey<T> key, T value)
            : base(new PermutationParameterUpdater<T>(parameters, key), value)
        {
        }
    }

    sealed class ValueParameterPin<T> : ParameterPin<T, ValueParameterKey<T>>
        where T : struct
    {
        public ValueParameterPin(ParameterCollection parameters, ValueParameterKey<T> key, T value)
            : base(new ValueParameterUpdater<T>(parameters, key), value)
        {
        }
    }

    sealed class EnumParameterPin<T, TEnum> : IVLPin<TEnum> 
        where T : unmanaged 
        where TEnum : unmanaged
    {
        private readonly ValueParameterPin<T> pin;

        public EnumParameterPin(ParameterCollection parameters, ValueParameterKey<T> key, TEnum value)
        {
            pin = new ValueParameterPin<T>(parameters, key, Unsafe.As<TEnum, T>(ref value));
        }

        public TEnum Value
        {
            get
            {
                T val = pin.Value;
                return Unsafe.As<T, TEnum>(ref val);
            }
            set => pin.Value = Unsafe.As<TEnum, T>(ref value);
        }

        object IVLPin.Value
        {
            get => Value;
            set => Value = (TEnum)value;
        }
    }

    sealed class ColorParameterPin : IVLPin<Color4>
    {
        private readonly ValueParameterPin<Color4> pin;

        public readonly ColorSpace ColorSpace;

        public ColorParameterPin(ParameterCollection parameters, ValueParameterKey<Color4> key, ColorSpace colorSpace, Color4 value)
        {
            pin = new ValueParameterPin<Color4>(parameters, key, value.ToColorSpace(colorSpace));
            ColorSpace = colorSpace;
        }

        public Color4 Value
        {
            get => pin.Value;
            set => pin.Value = value.ToColorSpace(ColorSpace);
        }

        object IVLPin.Value
        {
            get => Value;
            set => Value = (Color4)value;
        }
    }

    sealed class ArrayValueParameterPin<T> : ParameterPin<T[], ValueParameterKey<T>>
        where T : struct
    {
        public ArrayValueParameterPin(ParameterCollection parameters, ValueParameterKey<T> key, T[] value) 
            : base(new ArrayParameterUpdater<T>(parameters, key), value)
        {
        }
    }

    sealed class ResourceParameterPin<T> : ParameterPin<T, ObjectParameterKey<T>>
        where T : class
    {
        public ResourceParameterPin(ParameterCollection parameters, ObjectParameterKey<T> key)
            : base(new ObjectParameterUpdater<T>(parameters, key), default)
        {
        }
    }

    abstract class ShaderFXPin : ParameterPin
    {
        public readonly PermutationParameterKey<ShaderSource> Key;

        public ShaderFXPin(PermutationParameterKey<ShaderSource> key)
        {
            Key = key;
        }

        public bool ShaderSourceChanged { get; set; } = true;

        public void GenerateAndSetShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys, ShaderMixinSource mixin = null)
        {
            var shaderSource = GetShaderSource(context, baseKeys);

            context.Parameters.Set(Key, shaderSource);

            if (mixin != null)
            {
                mixin.Compositions[Key.Name] = shaderSource;
            }

            ShaderSourceChanged = false; //change seen
        }

        protected abstract ShaderSource GetShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys);

        public abstract IComputeNode GetValueOrDefault();

        internal override sealed void SubscribeTo(ShaderGeneratorContext c)
        {
            // We're part of the shader graph -> GetShaderSource takes care of writing the immutable shader source to the parameters
            // Should the shader source change a new graph will be generated (ShaderSourceChanged == true)
        }
    }

    class ShaderFXPin<TShaderClass> : ShaderFXPin, IVLPin<TShaderClass> where TShaderClass : class, IComputeNode
    {
        private TShaderClass internalValue;
        protected TShaderClass defaultValue;

        public ShaderFXPin(PermutationParameterKey<ShaderSource> key, TShaderClass value)
            : base(key)
        {
            internalValue = value;
            defaultValue = value;
        }

        public TShaderClass Value
        {
            get => internalValue;
            set
            {
                if (internalValue != value)
                {
                    internalValue = value;
                    ShaderSourceChanged = true;
                }
            }
        }

        public override IComputeNode GetValueOrDefault()
        {
            return internalValue ?? defaultValue;
        }

        object IVLPin.Value
        {
            get => Value;
            set => Value = (TShaderClass)value;
        }

        protected override sealed ShaderSource GetShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var shaderSource = GetShaderSourceCore(context, baseKeys);

            context.Parameters.Set(Key, shaderSource);

            return shaderSource;
        }

        protected virtual ShaderSource GetShaderSourceCore(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            return Value?.GenerateShaderSource(context, baseKeys) ?? defaultValue?.GenerateShaderSource(context, baseKeys);
        }
    }

    sealed class GPUValueSinkPin<T> : ShaderFXPin<SetVar<T>>
    {
        public GPUValueSinkPin(PermutationParameterKey<ShaderSource> key, SetVar<T> value) 
            : base(key, value)
        {
        }

        protected override ShaderSource GetShaderSourceCore(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var input = Value ?? defaultValue;
            var getter = input.GetVarValue();
            var graph = ShaderGraph.BuildFinalShaderGraph(getter);
            var finalVar = new Do<T>(graph, getter);
            return finalVar.GenerateShaderSource(context, baseKeys);
        }

        public override IComputeNode GetValueOrDefault()
        {
            var input = Value ?? defaultValue;
            return input.GetVarValue();
        }
    }

    abstract class Pin : IVLPin
    {
        public Pin(string name)
        {
            Name = name;
        }

        public abstract object Value { get; set; }

        public string Name { get; }
    }

    class Pin<T> : Pin, IVLPin<T>
    {
        public Pin(string name, T initialValue) : base(name)
        {
            Value = initialValue;
        }

        T IVLPin<T>.Value { get; set; }

        public sealed override object Value
        {
            get => ((IVLPin<T>)this).Value;
            set => ((IVLPin<T>)this).Value = (T)value;
        }
    }
}
