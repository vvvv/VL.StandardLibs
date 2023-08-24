using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VL.Stride.Shaders.ShaderFX.Functions;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;

namespace VL.Stride.Shaders.ShaderFX
{
    public class RaymarcherMatcap : Funk1In1Out<Vector2, Vector4>
    {
        readonly ObjectParameterUpdater<Texture> updater = new ObjectParameterUpdater<Texture>(default(ShaderGeneratorContext));

        public RaymarcherMatcap(string functionName, IEnumerable<KeyValuePair<string, IComputeNode>> inputs)
            : base(functionName, inputs)
        {
        }

        /// <summary>
        /// Can be updated from mainloop
        /// </summary>
        public Texture Input
        {
            get => updater.Value;
            set => updater.Value = value;
        }

        public ObjectParameterKey<Texture> UsedKey { get; protected set; }
        public ObjectParameterKey<Texture> Key { get; }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var shaderSource = new ShaderClassSource(ShaderName);

            UsedKey = Key ?? TexturingKeys.Texture0;

            //track parameter collection
            updater.Track(context, UsedKey);

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
