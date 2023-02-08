using System;
using System.Collections.Generic;
using System.Text;
using Stride.Rendering.Materials;
using Stride.Shaders;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;

namespace VL.Stride.Shaders.ShaderFX.Control
{
    public class Do<T> : ComputeValue<T>
    {
        public Do(IComputeVoid before, IComputeValue<T> value)
        {
            Before = before;
            Value = value;
        }

        public IComputeVoid Before { get; }
        public IComputeValue<T> Value { get; }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var shaderClassSource = GetShaderSourceForType<T>("Do");

            var mixin = shaderClassSource.CreateMixin();

            mixin.AddComposition(Before, "Before", context, baseKeys);
            mixin.AddComposition(Value, "Value", context, baseKeys);

            return mixin;
        }

        public override IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            return ReturnIfNotNull(Before, Value);
        }
    }
}
