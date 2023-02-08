using System.Collections.Generic;
using System.Text;
using Stride.Rendering.Materials;
using Stride.Shaders;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;


namespace VL.Stride.Shaders.ShaderFX
{
    public interface IComputeValue<T> : IComputeNode
    {

    }

    public class ComputeValue<T> : ComputeNode<T>, IComputeValue<T>
    {
        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            return GetShaderSourceForType<T>(ShaderName);
        }
    }
}
