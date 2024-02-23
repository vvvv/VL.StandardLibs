using System;
using System.Collections.Generic;
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
    [Monadic(typeof(GpuMonadicFactory<>))]
    public class SetVar<T> : VarBase<T>, IComputeVoid
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
            return new GpuValueBuilder<T>();
        }
    }

    public sealed class GpuValueBuilder<T> : IMonadBuilder<T, SetVar<T>>, IDisposable
        where T : unmanaged
    {
        private readonly InputValue<T> inputValue;
        private readonly SetVar<T> gpuValue;
        private readonly IResourceHandle<GraphicsDevice> graphicsDeviceHandle;

        public GpuValueBuilder()
        {
            inputValue = new InputValue<T>();
            gpuValue = DeclAndSetVar("Input", inputValue);
            graphicsDeviceHandle = AppHost.Current.Services.GetDeviceHandle();
        }

        public void Dispose()
        {
            graphicsDeviceHandle.Dispose();
        }

        public SetVar<T> Return(T value)
        {
            if (typeof(T) == typeof(Color4))
            {
                // We do the same as what the ColorIn node does in its default settings
                var device = graphicsDeviceHandle.Resource;
                var color = Unsafe.As<T, Color4>(ref value);
                var deviceColor = Color4.PremultiplyAlpha(color.ToColorSpace(device.ColorSpace));
                inputValue.Input = Unsafe.As<Color4, T>(ref deviceColor);
            }
            else
            {
                inputValue.Input = value;
            }
            return gpuValue;
        }
    }
}
