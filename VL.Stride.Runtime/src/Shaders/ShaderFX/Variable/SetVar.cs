using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Rendering.Materials;
using Stride.Shaders;
using VL.Core;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;


namespace VL.Stride.Shaders.ShaderFX
{
    /// <summary>
    /// Defines a variable and assigns a value to it. Can also re-assign an existing Var.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="IComputeVoid" />
    [Monadic(typeof(GpuMonadicFactory<>))]
    public class SetVar<T> : VarBase<T>, IComputeVoid
        where T : unmanaged
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
            if (Declaration is DeclConstant<T> constant)
                return string.Format("Constant {0}", constant.VarName);
            else if (Declaration is DeclSemantic<T> semantic)
                return string.Format("Get Semantic {0}", semantic.SemanticName);
            return string.Format("Assign {0} ", Declaration.VarName);
        }
    }

    public sealed class GpuMonadicFactory<T> : IMonadicFactory<T, SetVar<T>>
        where T : unmanaged
    {
        public static readonly GpuMonadicFactory<T> Default = new GpuMonadicFactory<T>();

        public IMonadBuilder<T, SetVar<T>> GetMonadBuilder(bool isConstant)
        {
            return (IMonadBuilder<T, SetVar<T>>)Activator.CreateInstance(typeof(GpuValueBuilder<>).MakeGenericType(typeof(T)));
        }
    }

    public sealed class GpuValueBuilder<T> : IMonadBuilder<T, SetVar<T>>
        where T : unmanaged
    {
        private readonly InputValue<T> inputValue;
        private readonly SetVar<T> gpuValue;

        public GpuValueBuilder()
        {
            inputValue = new InputValue<T>();
            gpuValue = DeclAndSetVar("Input", inputValue);
        }

        public SetVar<T> Return(T value)
        {
            inputValue.Input = value;
            return gpuValue;
        }
    }
}
