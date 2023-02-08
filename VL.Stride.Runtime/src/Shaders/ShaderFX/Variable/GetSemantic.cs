using System;
using System.Collections.Generic;
using System.Text;
using Stride.Rendering.Materials;
using Stride.Shaders;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;


namespace VL.Stride.Shaders.ShaderFX
{
    public class GetSemantic<T> : GetVar<T>
    {
        public GetSemantic(string semantic, string name = "SemanticValue")
            : base(new DeclSemantic<T>(semantic, name))
        {
        }

        public override string ToString()
        {
            return string.Format("Get {0}", Declaration.ToString());
        }
    }
}
