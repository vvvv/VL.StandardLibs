using System;
using System.Collections.Generic;
using System.Text;

namespace VL.Stride.Shaders.ShaderFX.Functions
{
    public interface IFunk1In1OutDefinition<TIn, TOut>
    {
        SetVar<TOut> BuildFunk(SetVar<TIn> arg);
    }
}
