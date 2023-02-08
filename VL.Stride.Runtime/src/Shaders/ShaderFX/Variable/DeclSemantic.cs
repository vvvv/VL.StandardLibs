using System;
using System.Collections.Generic;
using System.Text;
using Stride.Rendering.Materials;
using Stride.Shaders;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;


namespace VL.Stride.Shaders.ShaderFX
{
    public class DeclSemantic<T> : DeclVar<T>
    {
        public DeclSemantic(string semantic, string name = "SemanticValue")
            : base(name, appendID: true)
        {
            SemanticName = semantic;
        }

        public string SemanticName { get; }

        public override string ToString()
        {
            return string.Format("Decl Semantic {0}", SemanticName);
        }
    }
}
