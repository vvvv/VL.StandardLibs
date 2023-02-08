using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Disposables;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;

namespace VL.Stride.Shaders.ShaderFX
{
    public class GenericComputeNode<TOut> : ComputeValue<TOut>
    {
        ShaderClassCode shaderClass;

        public GenericComputeNode(
            Func<ShaderGeneratorContext, MaterialComputeColorKeys, ShaderClassCode> getShaderSource,
            IEnumerable<KeyValuePair<string, IComputeNode>> inputs)
        {
            Inputs = inputs?.Where(input => !string.IsNullOrWhiteSpace(input.Key) && input.Value != null).ToList();
            GetShaderSource = getShaderSource;
            ParameterCollections = ImmutableArray<ParameterCollection>.Empty;
        }

        public Func<ShaderGeneratorContext, MaterialComputeColorKeys, ShaderClassCode> GetShaderSource { get; }

        public ImmutableArray<ParameterCollection> ParameterCollections { get; private set; }

        public IEnumerable<KeyValuePair<string, IComputeNode>> Inputs { get; }

        public ShaderClassCode ShaderClass => shaderClass;

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            shaderClass = GetShaderSource(context, baseKeys);

            // TODO: Remove me - didn't know that this was already deprecated
            //store the parameters - accessed by various patches (look for InputParameterManager)
            var parameters = context.Parameters;
            if (context.TryGetSubscriptions(out var s))
            {
                ParameterCollections = ParameterCollections.Add(parameters);
                s.Add(Disposable.Create(() => ParameterCollections = ParameterCollections.Remove(parameters)));
            }

            //compose if necessary
            if (Inputs != null && Inputs.Any())
            {
                var mixin = shaderClass.CreateMixin();

                foreach (var input in Inputs)
                {
                    mixin.AddComposition(input.Value, input.Key, context, baseKeys);
                }

                return mixin;
            }

            return shaderClass;
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
            return shaderClass?.ToString() ?? GetType().ToString();
        }
    }
}
