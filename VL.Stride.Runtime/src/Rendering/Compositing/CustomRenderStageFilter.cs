using Stride.Rendering;
using System;

namespace VL.Stride.Rendering.Compositing
{
    public sealed class CustomRenderStageFilter : RenderStageFilter
    {
        public Func<RenderObject, RenderView, RenderViewStage, bool> IsVisibleFunc { get; set; }

        public override bool IsVisible(RenderObject renderObject, RenderView renderView, RenderViewStage renderViewStage)
        {
            if (IsVisibleFunc != null)
                return IsVisibleFunc(renderObject, renderView, renderViewStage);
            else
                return false;
        }
    }
}
