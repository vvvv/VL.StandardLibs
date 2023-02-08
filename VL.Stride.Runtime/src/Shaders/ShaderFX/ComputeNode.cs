using System;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;
using Stride.Core.Mathematics;
using System.Collections.Generic;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;

namespace VL.Stride.Shaders.ShaderFX
{
    public class ComputeNode<T> : ComputeNode, IComputeValue<T>
    {
        public string ShaderName { get; set; } = "Compute";

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var shaderSource = GetShaderSourceForType<T>(ShaderName);
            return shaderSource;
        }
    }
}
