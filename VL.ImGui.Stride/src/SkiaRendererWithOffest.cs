using VL.Skia;
using SkiaSharp;
using Stride.Core.Mathematics;

namespace VL.ImGui
{
    using SkiaRenderer = VL.Stride.SkiaRenderer;

    internal class SkiaRendererWithOffset : SkiaRenderer
    {
        public ILayer beforeTransformLayer { get; private set; }
        readonly TransformUpstream transformUpstream;

        public SkiaRendererWithOffset(ILayer layer) : base()
        {
            beforeTransformLayer = layer;
            transformUpstream = new TransformUpstream();
            Layer = layer;
        }

        public void SetOffset(Vector2 off)
        {
            var x = off.X * ImGuiConversion.FromImGuiScaling;
            var y = off.Y * ImGuiConversion.FromImGuiScaling;
            transformUpstream.Update(beforeTransformLayer, SKMatrix.CreateTranslation(-x, -y), out ILayer outLayer);
            Layer = outLayer;
        }
    }
}
