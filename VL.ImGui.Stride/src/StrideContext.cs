using Stride.Graphics;
using Stride.Rendering;
using VL.ImGui.Widgets;
using VL.Skia;

namespace VL.ImGui
{
    using SkiaRenderer = VL.Stride.SkiaRenderer;

    internal interface IContextWithRenderer
    {
        public IntPtr AddRenderer(RenderLayer renderer);

        public void RemoveRenderer(RenderLayer renderer);
    }

    internal sealed class StrideContext : Context, IContextWithSkia, IContextWithRenderer
    {
        public readonly List<RenderLayer> Renderers = new List<RenderLayer>();
        private readonly List<SkiaRenderer> managedSkiaRenderers = new();

        public IntPtr AddLayer(SkiaWidget layer, System.Numerics.Vector2 pos, System.Numerics.Vector2 size)
        {
            var renderer = Renderers.Where(r => r.Layer is SkiaRenderer skia && skia.Layer == layer).FirstOrDefault();

            if (renderer != null)
            {
                return Renderers.IndexOf(renderer) + 1;
            }
            else
            {
                var skiaRenderer = new SkiaRenderer()
                {
                    Space = CommonSpace.DIPTopLeft
                };

                // We take ownership
                managedSkiaRenderers.Add(skiaRenderer);

                InViewportUpstream viewportLayer = new InViewportUpstream();
                SetSpaceUpstream2 withinCommonSpaceLayer = new SetSpaceUpstream2();


                skiaRenderer.Layer = layer;

                var viewPort = new Viewport(pos.X, pos.Y, size.X, size.Y);

                RenderLayer renderLayer = new RenderLayer();
                renderLayer.Layer = skiaRenderer;
                renderLayer.Viewport = viewPort;

                Renderers.Add(renderLayer);
                return Renderers.Count;
            }
        }

        public void RemoveLayer(SkiaWidget layer)
        {
            var renderer = Renderers.Where(r => r.Layer is SkiaRenderer skia && skia.Layer == layer).FirstOrDefault();
            if (renderer != null)
            {
                RemoveRenderer(renderer);
            }
        }

        public IntPtr AddRenderer(RenderLayer renderer)
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

        public void RemoveRenderer(RenderLayer renderer)
        {
            if (Renderers.Remove(renderer))
                OnRemoved(renderer);
        }

        private void OnRemoved(RenderLayer renderLayer)
        {
            if (renderLayer.Layer is SkiaRenderer skiaRenderer)
            {
                if (managedSkiaRenderers.Remove(skiaRenderer))
                    skiaRenderer.Dispose();
            }
        }

        public RenderLayer? GetRenderer(IntPtr index)
        {
            if (Renderers.Count >= index)
                return Renderers.ElementAt((int)index - 1);
            else
                return null;
        }

        public override void Dispose()
        {
            foreach (var r in Renderers)
                OnRemoved(r);

            Renderers.Clear();

            base.Dispose();
        }
    }

    internal sealed class RenderLayer
    {
        public IGraphicsRendererBase? Layer { get; set; }
        public RenderView? RenderView { get; set; }
        public Viewport? Viewport { get; set; }
    }
}
