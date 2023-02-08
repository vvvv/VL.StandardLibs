using System;
using System.Runtime.Serialization;
using System.Text;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace VL.Stride.Shaders.ShaderFX
{
    public interface IComputeVoid : IComputeNode
    {

    }

    public abstract class ComputeVoid : ComputeNode, IComputeVoid
    {

    }
}
