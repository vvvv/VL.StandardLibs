using System;
using System.Collections.Generic;
using System.Text;

namespace VL.Stride.Shaders.ShaderFX.Functions
{
    public interface IFunk1In1OutDefinition<TIn, TOut>
        where TIn : unmanaged
        where TOut : unmanaged
    {
        SetVar<TOut> BuildFunk(SetVar<TIn> arg);
    }
}
