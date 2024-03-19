using SkiaSharp;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using VL.Core;
using VL.ImGui.Widgets;
using VL.Lib.Basics.Resources;
using VL.Lib.IO.Notifications;
using VL.Skia;
using VL.Stride;
using VL.Stride.Input;
using InputManager = Stride.Input.InputManager;

namespace VL.ImGui
{
    using SkiaRenderer = VL.Stride.SkiaRenderer;
    
    internal interface IContextWithRenderer
    {
        public IntPtr AddRenderer(RenderLayerWithInputSource renderer);

        public void RemoveRenderer(RenderLayerWithInputSource renderer);
    }

    internal sealed class StrideContext : Context, IContextWithSkia, IContextWithRenderer
    {
        public StrideContext(InputManager inputManager) : base()
        {
            this.inputManager = inputManager;
        }

        private readonly List<RenderLayerWithInputSource> Renderers = new List<RenderLayerWithInputSource>();
        private readonly List<SkiaRenderer> managedSkiaRenderers = new();
        internal InputManager inputManager;
        private Int2 rendertargetSize;

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

                RenderLayerWithInputSource renderLayer = new RenderLayerWithInputSource(this.inputManager);
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

        public IntPtr AddRenderer(RenderLayerWithInputSource renderer)
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

        public void RemoveRenderer(RenderLayerWithInputSource renderer)
        {
            if (Renderers.Remove(renderer))
                OnRemoved(renderer);
        }

        private void OnRemoved(RenderLayerWithInputSource renderLayer)
        {
            if (renderLayer.Layer is SkiaRenderer skiaRenderer)
            {
                if (managedSkiaRenderers.Remove(skiaRenderer))
                    skiaRenderer.Dispose();
            }
            renderLayer.Dispose();
        }

        public RenderLayerWithInputSource? GetRenderer(IntPtr index)
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
