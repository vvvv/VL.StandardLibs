using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Stride.Core;
using Stride.Rendering.Materials;
using Stride.Shaders;
using DataMemberAttribute = Stride.Core.DataMemberAttribute;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;
using System.Linq;

namespace VL.Stride.Shaders.ShaderFX
{

    public class MultiInputOperation<TOut> : ComputeValue<TOut>
    {
        public MultiInputOperation(string operatorName, IEnumerable<ShaderInput> inputs, bool nameOnlyDependsOnOutput, params object[] genericArguments)
        {
            ShaderName = operatorName;
            Inputs = inputs.ToList();
            GenericArguments = genericArguments;
            NameOnlyDependsOnOutput = nameOnlyDependsOnOutput;
        }

        List<ShaderInput> Inputs { get; }
        public object[] GenericArguments { get; }

        public bool NameOnlyDependsOnOutput { get; }

        public override IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            return ReturnIfNotNull(Inputs.Select(i => i.Shader));
        }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var shaderSource = NameOnlyDependsOnOutput ? 
                GetShaderSourceForType<TOut>(ShaderName, GenericArguments) : 
                GetShaderSourceForInputs<TOut>(ShaderName, Inputs.Select(i => i.Shader), GenericArguments);

            var mixin = shaderSource.CreateMixin();

            foreach (var cs in Inputs)
            {
                mixin.AddComposition(cs.Shader, cs.CompositionName, context, baseKeys);
            }

            return mixin;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", ShaderName, typeof(TOut));
        }
    }
}
