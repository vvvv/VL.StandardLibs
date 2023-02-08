using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Stride.Core;
using Stride.Rendering.Materials;
using Stride.Shaders;
using DataMemberAttribute = Stride.Core.DataMemberAttribute;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;

namespace VL.Stride.Shaders.ShaderFX
{
    public class UnaryOperation<T> : ComputeValue<T>
    {
        public UnaryOperation(string operatorName, IComputeValue<T> value)
        {
            ShaderName = operatorName;
            Value = value;
        }

        public IComputeValue<T> Value { get; }

        public override IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            return ReturnIfNotNull(Value);
        }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var shaderSource = GetShaderSourceForType<T>(ShaderName);

            var mixin = shaderSource.CreateMixin();

            mixin.AddComposition(Value, "Value", context, baseKeys);

            return mixin;
        }

        public override string ToString()
        {
            return string.Format("Op {0} {1}", Value, ShaderName);
        }
    }
}
