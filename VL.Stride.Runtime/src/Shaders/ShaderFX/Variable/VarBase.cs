using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;


namespace VL.Stride.Shaders.ShaderFX
{
    /// <summary>
    /// Base class for get or assign a value to a stream variable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class VarBase<T> : ComputeNode
    {
        public VarBase(DeclVar<T> declaration)
        {
            Declaration = declaration;
        }

        public DeclVar<T> Declaration { get; }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            Declaration.GetNameForContext(context);
            return GetShaderSourceForType<T>("Compute");
        }

        public override string ToString()
        {
            return string.Format("Var: {0}", Declaration.VarName);
        }
    }
}
