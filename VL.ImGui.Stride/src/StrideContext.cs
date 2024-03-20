using Stride.Graphics;
using Stride.Input;
using System.Reactive.Linq;
using VL.ImGui.Widgets;
using VL.Lib.Basics.Resources;
using VL.Skia;
using InputManager = Stride.Input.InputManager;

namespace VL.ImGui
{
    using SkiaRenderer = VL.Stride.SkiaRenderer;
    
    internal interface IContextWithRenderer
    {
        public IntPtr AddRenderer(RenderLayerWithViewPort renderer);

        public void RemoveRenderer(RenderLayerWithViewPort renderer);
    }

    internal sealed class StrideContext : Context, IContextWithSkia, IContextWithRenderer
    {
        public StrideContext(InputManager inputManager) : base()
        {
            this.inputManager = inputManager;
        }

        private readonly List<RenderLayerWithViewPort> Renderers = new List<RenderLayerWithViewPort>();
        private readonly List<SkiaRenderer> managedSkiaRenderers = new();
        internal InputManager inputManager;

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

                skiaRenderer.Layer = layer;

                RenderLayerWithViewPort renderLayer = new RenderLayerWithViewPort();
                renderLayer.Layer = skiaRenderer;
                renderLayer.Viewport = new Viewport(pos.X, pos.Y, size.X, size.Y);

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
            if (renderLayer.Layer is SkiaRenderer skiaRenderer)
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
