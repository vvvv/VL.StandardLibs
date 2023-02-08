using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Rendering.Materials;
using Stride.Shaders;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;


namespace VL.Stride.Shaders.ShaderFX
{
    /// <summary>
    /// Contains information about a stream variable and generates a unique but stable ID when the shader gets compiled.
    /// </summary>
    public class DeclVar<T>
    {
        private string varNameWithID;

        public DeclVar(string varName, bool appendID = true)
        {
            VarName = varName;
            AppendID = appendID;
        }

        public virtual string GetNameForContext(ShaderGeneratorContext context)
        {
            if (AppendID)
            {
                varNameWithID = context.GetNameForContext(this, varNameWithID, VarName);
            }
            else
            {
                varNameWithID = VarName;
            }

            return varNameWithID;
        }

        public IComputeValue<T> Value { get; }

        public string VarName { get; }

        public bool AppendID { get; }

        public override string ToString()
        {
            return string.Format("Decl Var: {0}", VarName);
        }
    }
}
