using System;
using System.Collections.Generic;
using System.Text;
using Stride.Rendering.Materials;
using Stride.Shaders;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;

namespace VL.Stride.Shaders.ShaderFX.Control
{
    public class IfThen : ComputeVoid
    {
        public IfThen(IComputeVoid then, IComputeValue<bool> condition)
        {
            Then = then;
            Condtion = condition;
        }

        public IComputeVoid Then { get; }
        public IComputeValue<bool> Condtion { get; }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var shaderClassSource = new ShaderClassSource("IfThen");

            var mixin = shaderClassSource.CreateMixin();

            mixin.AddComposition(Then, "Then", context, baseKeys);
            mixin.AddComposition(Condtion, "Condition", context, baseKeys);

            return mixin;
        }

        public override IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            return ReturnIfNotNull(Then, Condtion);
        }
    }
}
