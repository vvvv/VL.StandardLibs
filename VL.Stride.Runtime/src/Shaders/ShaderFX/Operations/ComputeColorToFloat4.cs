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
    public class ComputeColorToFloat4 : ComputeValue<Vector4>
    {
        public ComputeColorToFloat4(IComputeColor value)
        {
            Value = value;
        }

        public IComputeColor Value { get; }

        public override IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            return ReturnIfNotNull(Value);
        }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var shaderSource = new ShaderClassSource("ColorToFloat4");

            var mixin = shaderSource.CreateMixin();

            mixin.AddComposition(Value, "Value", context, baseKeys);

            return mixin;
        }
    }
}
