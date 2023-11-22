using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Rendering.Materials;
using Stride.Shaders;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;


namespace VL.Stride.Shaders.ShaderFX.Functions
{
    public class Delegate1In1Out<TIn, TOut> : Funk1In1Out<TIn, TOut>
        where TIn : unmanaged
        where TOut : unmanaged
    {
        public Delegate1In1Out(SetVar<TIn> arg, SetVar<TOut> result, IComputeVoid body)
            : base("Delegate", null)
        {
            Arg = arg;
            Result = result;

            if (body != null)
            {
                Inputs = new[]
                {
                    new KeyValuePair<string, IComputeNode>("Body", body)
                }; 
            }
        }

        public new IEnumerable<KeyValuePair<string, IComputeNode>> Inputs { get; private set; }
        public SetVar<TIn> Arg { get; }
        public SetVar<TOut> Result { get; }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var shaderSource = GetShaderSourceFunkForType2<TIn, TOut>(ShaderName, Arg.Declaration.GetNameForContext(context), Result.Declaration.GetNameForContext(context));

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
