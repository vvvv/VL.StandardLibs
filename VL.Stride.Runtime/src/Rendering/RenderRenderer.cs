using Stride.Core.Mathematics;
using Stride.Rendering;

namespace VL.Stride.Rendering
{
    /// <summary>
    /// The render object used by the low level rendering system.
    /// </summary>
    public class RenderRenderer : RenderObject
    {
        public bool SingleCallPerFrame;
        public DrawerRenderStage RenderStage;
        public Matrix ParentTransformation;
        public IGraphicsRendererBase Renderer;
    }
}
