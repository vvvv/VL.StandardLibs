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
    public class GetConstant<T> : GetVar<T>
        where T : unmanaged
    {
        public T ConstantValue { get ; }

        public GetConstant(T value)
            : base(new DeclConstant<T>(value))
        {
            ConstantValue = value;
        }

        public override string ToString()
        {
            return string.Format("Get {0}", Declaration.ToString());
        }
    }
}
