using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.Materials;
using Stride.Shaders;
using VL.Core;
using VL.Lib.Basics.Resources;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;


namespace VL.Stride.Shaders.ShaderFX
{
    /// <summary>
    /// Defines a variable and assigns a value to it. Can also re-assign an existing Var.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="IComputeVoid" />
    [MonadicTypeFilter(typeof(GpuMonadicTypeFilter))]
    public class SetVar<T> : VarBase<T>, IComputeVoid, IMonadicValue<T>
    {
        public SetVar(IComputeValue<T> value, DeclVar<T> declaration, bool evaluateChildren = true)
            : base(declaration)
        {
            Var = null;
            Value = value;
            EvaluateChildren = evaluateChildren;
        }

        public SetVar(IComputeValue<T> value, SetVar<T> var, bool evaluateChildren = true)
            : base(var.Declaration)
        {
            Var = var;
            Value = value;
            EvaluateChildren = evaluateChildren;
        }

        [DataMemberIgnore]
        public SetVar<T> Var { get; }

        public IComputeValue<T> Value { get; }

        public bool EvaluateChildren { get; }

#nullable enable
        T? IMonadicValue<T>.Value 
        {
            get => Value is IInputValue<T> inputValue ? inputValue.Input : default;
        }

        IMonadicValue<T> IMonadicValue<T>.SetValue(T? value)
        {
            if (Value is IInputValue<T> inputValue)
                inputValue.Input = value!;
            return this;
        }

        bool IMonadicValue.HasValue => Value is IInputValue<T> i && i.HasValue;

        bool IMonadicValue.AcceptsValue => Value is IInputValue<T>;
#nullable restore

        public override IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            return EvaluateChildren ? ReturnIfNotNull(Var, Value) : ReturnIfNotNull();
        }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            if (Value is null) //nothing will be set, i.e. declaration is a constant or an existing semantic
                return new ShaderClassSource("ComputeVoid");

            var varName = Declaration.GetNameForContext(context);

            var shaderSource = Declaration is DeclSemantic<T> semantic
                ? GetShaderSourceForType<T>("AssignSemantic", varName, semantic.SemanticName)
                : GetShaderSourceForType<T>("AssignVar", varName);

            var mixin = shaderSource.CreateMixin();

            mixin.AddComposition(Value, "Value", context, baseKeys);

            return mixin;
        }

        public override string ToString()
        {
            if (Value is IInputValue<T> inputValue) 
                return inputValue.ToString();

            if (Declaration is DeclConstant<T> constant)
                return string.Format("Constant {0}", constant.VarName);
            else if (Declaration is DeclSemantic<T> semantic)
                return string.Format("Get Semantic {0}", semantic.SemanticName);
            return string.Format("Assign {0} ", Declaration.VarName);
        }

        static IMonadicValue<T> IMonadicValue<T>.Create(NodeContext nodeContext, T value)
        {
            if (!typeof(T).IsValueType)
                throw new InvalidOperationException($"{typeof(T)} must be a value type");

            return Create_Generic((dynamic)value);
        }

        private static SetVar<TValue> Create_Generic<TValue>(TValue value)
            where TValue : unmanaged
        {
            var inputValue = new InputValue<TValue>(convertToDeviceColorSpace: true)
            {
                Input = value,
                HasValue = true
            };
            return DeclAndSetVar("Input", inputValue);
        }
    }

    sealed class GpuMonadicTypeFilter : IMonadicTypeFilter
    {
        public bool Accepts(TypeDescriptor typeDescriptor)
        {
            return typeDescriptor.IsValueType;
        }
    }
}
