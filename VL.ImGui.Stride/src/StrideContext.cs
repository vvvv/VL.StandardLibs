using Stride.Graphics;
using Stride.Rendering;

namespace VL.ImGui
{
    internal sealed class StrideContext : Context
    {

    }

    internal sealed class RenderLayer
    {
        public IGraphicsRendererBase? Layer { get; set; }
        public RenderView? RenderView { get; set; }
        public Viewport? Viewport { get; set; }
    }



}
