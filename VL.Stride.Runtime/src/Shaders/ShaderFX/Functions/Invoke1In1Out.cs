using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Rendering.Materials;
using Stride.Shaders;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;


namespace VL.Stride.Shaders.ShaderFX.Functions
{
    public class Invoke1In1Out<TIn, TOut> : ComputeValue<TOut>
    {
        public Invoke1In1Out(Funk1In1Out<TIn, TOut> funk, IComputeValue<TIn> arg)
        {
            ShaderName = "Invoke";
            Inputs = new[] 
                { 
                    new KeyValuePair<string, IComputeNode>("Funk", funk), 
                    new KeyValuePair<string, IComputeNode>("Arg", arg) 
                };
        }

        public IEnumerable<KeyValuePair<string, IComputeNode>> Inputs { get; private set; }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var shaderSource = GetShaderSourceFunkForType2<TIn, TOut>(ShaderName);

            //compose if necessary
            if (Inputs != null && Inputs.Any())
            {
                var mixin = shaderSource.CreateMixin();

                foreach (var input in Inputs)
                {
                    mixin.AddComposition(input.Value, input.Key, context, baseKeys);
                }

                return mixin;
            }

            return shaderSource;
        }

        public override IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            if (Inputs != null)
            {
                foreach (var item in Inputs)
                {
                    if (item.Value != null)
                        yield return item.Value;
                }
            }
        }

        public override string ToString()
        {
            return ShaderName;
        }
    }
}
