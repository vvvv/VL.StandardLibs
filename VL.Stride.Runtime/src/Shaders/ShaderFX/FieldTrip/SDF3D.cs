using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Rendering.Materials;
using Stride.Shaders;
using Stride.Core.Mathematics;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;

namespace VL.Stride.Shaders.ShaderFX.Functions
{
    /// <summary>
    /// Represents any shader that implements SDF3D with input compositions
    /// </summary>
    public class SDF3D : Funk1In1Out<Vector3, float>
    {
        public SDF3D(string functionName, IEnumerable<KeyValuePair<string, IComputeNode>> inputs)
            : base(functionName, inputs)
        {
           
        }
    }
}
