using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Rendering.Materials;
using Stride.Shaders;
using DataMemberAttribute = Stride.Core.DataMemberAttribute;

namespace VL.Stride.Shaders.ShaderFX
{
    public class ComputeOrder : ComputeVoid
    {
        public ComputeOrder(IEnumerable<IComputeVoid> computes)
        {
            if (computes != null)
                Computes = computes.Where(c => c != null);
        }

        public ComputeOrder(params IComputeVoid[] computes)
        {
            if (computes != null)
                Computes = computes.Where(c => c != null);
        }

        /// <summary>
        /// The left (background) child node.
        /// </summary>
        /// <userdoc>
        /// The map used for the left (background) node.
        /// </userdoc>
        [DataMember(20)]
        [Display("Computes")]
        public IEnumerable<IComputeVoid> Computes { get; }

        public override IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            return Computes ?? base.GetChildren(context);
        }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var shaderSources = new ShaderArraySource();

            if (Computes != null)
                foreach (var compute in Computes)
                    shaderSources.Add(compute.GenerateShaderSource(context, baseKeys));

            var shaderSource = new ShaderClassSource("ComputeOrder");
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(shaderSource);
            mixin.AddComposition("Computes", shaderSources);

            return mixin;
        }
    }
}
