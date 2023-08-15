using System;
using Stride.Rendering.Materials;
using Stride.Shaders;
using System.Collections.Generic;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;
using System.Linq;

namespace VL.Stride.Shaders.ShaderFX
{
    // Do NOT inherit from Stride.Rendering.Materials.ComputeColors.ComputeNode as it crashes the Stride.Core.AssemblyProcessor
    // due to its inherited DataContract attribute
    public abstract class ComputeNode<T> : IComputeNode<T>
    {
        public string ShaderName { get; set; } = "Compute";

        public virtual ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var shaderSource = GetShaderSourceForType<T>(ShaderName);
            return shaderSource;
        }

        public virtual bool HasChanged => false;

        public virtual IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            return Enumerable.Empty<IComputeNode>();
        }
    }
}
