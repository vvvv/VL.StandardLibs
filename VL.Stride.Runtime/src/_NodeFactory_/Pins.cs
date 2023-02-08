using Stride.Core.Mathematics;
using Stride.Rendering.Materials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VL.Core;
using VL.Stride.Shaders.ShaderFX;
using VL.Stride.Shaders.ShaderFX.Control;

namespace VL.Stride
{
    abstract class PinDescription : IVLPinDescription, IInfo
    {
        protected readonly MemberInfo property;

        public PinDescription(MemberInfo property, string name)
        {
            this.property = property;
            Name = name;
        }

        public string Name { get; }

        public abstract Type Type { get; }

        public abstract object DefaultValue { get; }

        public string Summary => property.GetSummary();

        public string Remarks => property.GetRemarks();

        public abstract Pin<TInstance> CreatePin<TInstance>(StrideNode node) where TInstance : new();
    }

    abstract class PinDescription<T> : PinDescription
    {
        protected readonly T defaultValue;

        public PinDescription(MemberInfo property, string name, T defaultValue) 
            : base(property, name)
        {
            this.defaultValue = defaultValue;
        }

        public override Type Type => typeof(T);
    }

    sealed class ClassPinDec<T> : PinDescription<T>
        where T : class
    {
        public ClassPinDec(MemberInfo property, string name, T defaultValue) : base(property, name, defaultValue)
        {
        }

        // Called at compile time, value must be serializable
        public override object DefaultValue
        {
            get
            {
                if (typeof(T) == typeof(string))
                    return defaultValue;
                return null;
            }
        }

        public override Pin<TInstance> CreatePin<TInstance>(StrideNode node) => new ClassPin<TInstance, T>(node, property, defaultValue);
    }

    sealed class StructPinDec<T> : PinDescription<T> 
        where T : struct
    {
        public StructPinDec(MemberInfo property, string name, T defaultValue) : base(property, name, defaultValue)
        {
        }

        // Called at compile time, value must be serializable
        public override object DefaultValue => defaultValue;

        public override Pin<TInstance> CreatePin<TInstance>(StrideNode node) => new StructPin<TInstance, T>(node, property, defaultValue);
    }

    sealed class ListPinDesc<TList, TOriginal, TElement> : PinDescription<TList>
        where TList : class, IList<TElement> 
        where TOriginal : class, IList<TElement>
    {
        public ListPinDesc(MemberInfo property, string name, TList defaultValue) : base(property, name, defaultValue)
        {
        }

        public override object DefaultValue => null;

        public override Pin<TInstance> CreatePin<TInstance>(StrideNode node) => new ListPin<TInstance, TList, TOriginal, TElement>(node, property, defaultValue);
    }

    sealed class ComputeScalarPinDesc : PinDescription<SetVar<float>>
    {
        public ComputeScalarPinDesc(MemberInfo property, string name, SetVar<float> defaultValue) : base(property, name, defaultValue)
        {
        }

        public override object DefaultValue => defaultValue;

        public override Pin<TInstance> CreatePin<TInstance>(StrideNode node) => new ComputeScalarPin<TInstance>(node, property, defaultValue);
    }

    sealed class ComputeColorPinDesc : PinDescription<SetVar<Vector4>>
    {
        public ComputeColorPinDesc(MemberInfo property, string name, SetVar<Vector4> defaultValue) : base(property, name, defaultValue)
        {
        }

        public override object DefaultValue => defaultValue;

        public override Pin<TInstance> CreatePin<TInstance>(StrideNode node) => new ComputeColorPin<TInstance>(node, property, defaultValue);
    }

    abstract class Pin<TInstance> : IVLPin where TInstance : new()
    {
        protected readonly StrideNode node;

        public Pin(StrideNode node)
        {
            this.node = node;
        }

        public abstract object Value { get; set; }

        public abstract void ApplyValue(TInstance instance);
    }

    abstract class Pin<TInstance, TValue> : Pin<TInstance>, IVLPin<TValue> where TInstance : new()
    {
        private static readonly EqualityComparer<TValue> equalityComparer = EqualityComparer<TValue>.Default;

        protected readonly Func<TInstance, TValue> getter;
        protected readonly Action<TInstance, TValue> setter;
        protected readonly bool isReadOnly;
        protected TValue value;

        public Pin(StrideNode node, MemberInfo member, TValue value) : base(node)
        {
            getter = member.BuildGetter<TInstance, TValue>();

            if (member is PropertyInfo property)
                isReadOnly = property.SetMethod is null || !property.SetMethod.IsPublic;

            if (!isReadOnly)
                setter = member.BuildSetter<TInstance, TValue>();

            this.value = value;
        }

        public override object Value
        {
            get => ((IVLPin<TValue>)this).Value;
            set => ((IVLPin<TValue>)this).Value = (TValue)value;
        }

        TValue IVLPin<TValue>.Value
        {
            get => value;
            set
            {
                var valueT = value;
                if (!ValueEquals(valueT, this.value))
                {
                    this.value = valueT;
                    node.needsUpdate = true;
                }
            }
        }

        public override sealed void ApplyValue(TInstance instance)
        {
            ApplyCore(instance, value);
        }

        protected virtual void ApplyCore(TInstance instance, TValue value)
        {
            if (isReadOnly)
            {
                var dst = getter(instance);
                CopyProperties(value, dst);
            }
            else
            {
                setter(instance, value);
            }
        }

        protected virtual bool ValueEquals(TValue a, TValue b) => equalityComparer.Equals(a, b);

        private static void CopyProperties(object src, object dst)
        {
            foreach (var x in typeof(TValue).GetStrideProperties())
            {
                var p = x.Property as PropertyInfo;
                if (p.SetMethod != null && p.SetMethod.IsPublic && p.GetMethod != null && p.GetMethod.IsPublic)
                    p.SetValue(dst, p.GetValue(src));
            }
        }
    }

    abstract class ConversionPin<TInstance, TValue, TOriginal> : Pin<TInstance>, IVLPin<TValue> where TInstance : new()
    {
        private static readonly EqualityComparer<TValue> equalityComparer = EqualityComparer<TValue>.Default;

        protected readonly Func<TInstance, TOriginal> getter;
        protected readonly Action<TInstance, TOriginal> setter;
        protected readonly Func<TValue, TOriginal> valueToOriginal;
        protected readonly bool isReadOnly;
        protected TValue value;
        protected readonly TValue defaultValue;

        public ConversionPin(StrideNode node, MemberInfo member, TValue value) : base(node)
        {
            getter = member.BuildGetter<TInstance, TOriginal>();

            if (member is PropertyInfo property)
                isReadOnly = property.SetMethod is null || !property.SetMethod.IsPublic;

            if (!isReadOnly)
                setter = member.BuildSetter<TInstance, TOriginal>();

            valueToOriginal = GetValueToOriginal();

            defaultValue = this.value = value;
        }

        public override object Value
        {
            get => ((IVLPin<TValue>)this).Value;
            set => ((IVLPin<TValue>)this).Value = (TValue)value;
        }

        TValue IVLPin<TValue>.Value
        {
            get => value;
            set
            {
                var valueT = value;
                if (!ValueEquals(valueT, this.value))
                {
                    this.value = valueT;
                    node.needsUpdate = true;
                }
            }
        }

        public override sealed void ApplyValue(TInstance instance)
        {
            ApplyCore(instance, value);
        }

        protected abstract void ApplyCore(TInstance instance, TValue value);

        protected abstract Func<TValue, TOriginal> GetValueToOriginal();

        protected virtual bool ValueEquals(TValue a, TValue b) => equalityComparer.Equals(a, b);
    }

    sealed class ClassPin<TInstance, TValue> : Pin<TInstance, TValue> where TInstance : new()
        where TValue : class
    {
        readonly TValue defaultValue;

        public ClassPin(StrideNode node, MemberInfo property, TValue value) : base(node, property, value)
        {
            defaultValue = value;
        }

        protected override void ApplyCore(TInstance instance, TValue value)
        {
            base.ApplyCore(instance, value ?? defaultValue);
        }
    }

    sealed class StructPin<TInstance, TValue> : Pin<TInstance, TValue> where TInstance : new()
        where TValue : struct
    {
        public StructPin(StrideNode node, MemberInfo property, TValue value) : base(node, property, value)
        {
        }

        protected override void ApplyCore(TInstance instance, TValue value)
        {
            base.ApplyCore(instance, value);
        }
    }

    sealed class ListPin<TInstance, TList, TOriginal, TItem> : ConversionPin<TInstance, TList, TOriginal> where TInstance : new()
        where TList : class, IList<TItem>
        where TOriginal : class, IList<TItem>
    {
        public ListPin(StrideNode node, MemberInfo property, TList value) : base(node, property, value)
        {
        }

        protected override bool ValueEquals(TList a, TList b)
        {
            if (a is null)
                return b is null;
            if (b is null)
                return false;
            return a.SequenceEqual(b);
        }

        protected override void ApplyCore(TInstance instance, TList value)
        {
            var src = value ?? defaultValue;
            if (isReadOnly)
            {
                var dst = getter(instance) as IList<TItem>;
                dst.Clear();
                foreach (var item in src)
                    dst.Add(item);
            }
            else
            {
                setter(instance, valueToOriginal(src));
            }
        }

        protected override Func<TList, TOriginal> GetValueToOriginal()
        {
            return v => v as TOriginal;
        }
    }

    abstract class GPUValuePin<TInstance, TValue, TOriginal> : ConversionPin<TInstance, TValue, TOriginal> 
        where TInstance : new() 
        where TValue : IComputeNode
        where TOriginal : IComputeNode
    {
        public GPUValuePin(StrideNode node, MemberInfo property, TValue value) : base(node, property, value)
        {
            if (isReadOnly)
                throw new NotImplementedException("IComputeNode property is read-only: " + property.ToString());
        }

        protected override void ApplyCore(TInstance instance, TValue value)
        {
            var src = value ?? defaultValue;
            setter(instance, valueToOriginal(src));
        }
    }

    sealed class ComputeScalarPin<TInstance> : GPUValuePin<TInstance, SetVar<float>, IComputeScalar>
        where TInstance : new()
    {
        public ComputeScalarPin(StrideNode node, MemberInfo property, SetVar<float> value) : base(node, property, value)
        {
        }

        protected override Func<SetVar<float>, IComputeScalar> GetValueToOriginal()
        {
            return v =>
            {
                var input = v ?? ShaderFXUtils.Constant<float>(0);
                var getter = ShaderFXUtils.GetVarValue(input);
                var graph = ShaderGraph.BuildFinalShaderGraph(getter);
                var finalVar = new Do<float>(graph, getter);
                return new FloatToComputeFloat(finalVar);
            };
        }
    }

    sealed class ComputeColorPin<TInstance> : GPUValuePin<TInstance, SetVar<Vector4>, IComputeColor>
        where TInstance : new()
    {
        public ComputeColorPin(StrideNode node, MemberInfo property, SetVar<Vector4> value) : base(node, property, value)
        {
        }

        protected override Func<SetVar<Vector4>, IComputeColor> GetValueToOriginal()
        {
            return v =>
            {
                var input = v ?? ShaderFXUtils.Constant(Vector4.One);
                var getter = ShaderFXUtils.GetVarValue(input);
                var graph = ShaderGraph.BuildFinalShaderGraph(getter);
                var finalVar = new Do<Vector4>(graph, getter);
                return new Float4ToComputeColor(finalVar);
            };
        }
    }
}
