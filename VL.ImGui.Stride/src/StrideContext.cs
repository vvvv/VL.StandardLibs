using SkiaSharp;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Input;
using System.Reactive.Linq;
using VL.ImGui.Widgets;
using VL.Lib.Basics.Resources;
using VL.Lib.IO.Notifications;
using VL.Skia;

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
            transformUpstream.Update(beforeTransformLayer, SKMatrix.CreateTranslation(-x,-y), out ILayer outLayer);
            Layer = outLayer;
        }
    }

    internal interface IContextWithRenderer
    {
        public IntPtr AddRenderer(RenderLayerWithViewPort renderer);

        public void RemoveRenderer(RenderLayerWithViewPort renderer);
    }

    internal class StrideContext : Context, IContextWithSkia, IContextWithRenderer
    {
        public StrideContext() : base()
        {
        }

        private readonly List<RenderLayerWithViewPort> Renderers = new List<RenderLayerWithViewPort>();
        private readonly List<SkiaRendererWithOffset> managedSkiaRenderers = new();

        public IntPtr AddLayer(SkiaWidget layer, System.Numerics.Vector2 pos, System.Numerics.Vector2 size)
        {
            var renderer = Renderers.Where(r => r.Layer is SkiaRendererWithOffset skia && skia.beforeTransformLayer == layer).FirstOrDefault();

            if (renderer != null)
            {
                return Renderers.IndexOf(renderer) + 1;
            }
            else
            {
                var skiaRenderer = new SkiaRendererWithOffset(layer)
                {
                    Space = CommonSpace.DIPTopLeft,
                };

                // We take ownership
                managedSkiaRenderers.Add(skiaRenderer);

                RenderLayerWithViewPort renderLayer = new RenderLayerWithViewPort();
                renderLayer.Layer = skiaRenderer;
                renderLayer.Viewport = new Viewport(pos.X, pos.Y, size.X, size.Y);

                Renderers.Add(renderLayer);
                return Renderers.Count;
            }
        }

        public void RemoveLayer(SkiaWidget layer)
        {
            var renderer = Renderers.Where(r => r.Layer is SkiaRendererWithOffset skia && skia.beforeTransformLayer == layer).FirstOrDefault();
            if (renderer != null)
            {
                RemoveRenderer(renderer);
            }
        }

        public IntPtr AddRenderer(RenderLayerWithViewPort renderer)
        {
            if (Renderers.Contains(renderer))
            {
                return Renderers.IndexOf(renderer) + 1;
            }
            else
            {
                Renderers.Add(renderer);
                return Renderers.Count;
            }
        }

        public void RemoveRenderer(RenderLayerWithViewPort renderer)
        {
            if (Renderers.Remove(renderer))
                OnRemoved(renderer);
        }

        private void OnRemoved(RenderLayerWithViewPort renderLayer)
        {
            if (renderLayer.Layer is SkiaRendererWithOffset skiaRenderer)
            {
                if (managedSkiaRenderers.Remove(skiaRenderer))
                    skiaRenderer.Dispose();
            }
            renderLayer.Dispose();
        }

        public RenderLayerWithViewPort? GetRenderer(IntPtr index)
        {
            if (Renderers.Count >= index)
                return Renderers.ElementAt((int)index - 1);
            else
                return null;
        }

        public void WithInputSource(IInputSource? inputSource)
        {
            foreach (var renderer in Renderers)
            {
                renderer.ParentInputSource = inputSource;
            }
        }

        public override void Dispose()
        {
            foreach (var r in Renderers)
                OnRemoved(r);

            Renderers.Clear();

            base.Dispose();
        }
    }
}
