using System;
using System.Collections.Generic;
using System.Text;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;
using Stride.Core.Mathematics;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;


namespace VL.Stride.Shaders.ShaderFX
{
    public class Float4ToComputeColor : ComputeColor
    {
        public Float4ToComputeColor(IComputeValue<Vector4> value)
        {
            this.Value = value;
        }

        public IComputeValue<Vector4> Value { get; }

        public override IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            return ReturnIfNotNull(Value);
        }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var shaderSource = new ShaderClassSource("Float4ToColor");

            var mixin = shaderSource.CreateMixin();

            mixin.AddComposition(Value, "Value", context, baseKeys);

            return mixin;
        }
    }
}
