using Stride.Core.Mathematics;
using Stride.Rendering;

namespace VL.Stride.Rendering
{
    public interface IEffect
    {
        EffectInstance SetParameters(RenderView renderView, RenderDrawContext renderDrawContext);
    }
}
