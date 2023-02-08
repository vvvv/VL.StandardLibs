using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Stride.Rendering.Materials;
using Stride.Shaders;
using Stride.Core.Mathematics;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;


namespace VL.Stride.Shaders.ShaderFX
{
    public class DeclConstant<T> : DeclVar<T>
    {
        public readonly T ConstantValue;

        public DeclConstant(T value)
            : base("Constant")
        {
            ConstantValue = value;
        }

        public override string ToString()
        {
            return string.Format("Decl Constant {0}", GetAsShaderString(ConstantValue));
        }
    }
}
