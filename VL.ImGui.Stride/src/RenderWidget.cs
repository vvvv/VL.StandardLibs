using ImGuiNET;
using Stride.Core.Mathematics;
using Stride.Rendering;
using System.Runtime.InteropServices;
using Viewport = Stride.Graphics.Viewport;

namespace VL.ImGui.Widgets
{
    using ImGui = ImGuiNET.ImGui;

    [GenerateNode(Category = "ImGui.Widgets.Internal", IsStylable = false)]
    public sealed partial class RenderWidget : Widget, IDisposable
    {
        private readonly RenderLayer renderLayer;
        private readonly GCHandle renderLayerHandle;
        public RenderWidget()
        {
            renderLayer = new RenderLayer();
            renderLayerHandle = GCHandle.Alloc(renderLayer);
        }

        public IGraphicsRendererBase? Layer { private get; set ; }

        public RenderView? RenderView { private get; set; } = new RenderView();

        public Vector2 Size { private get; set; } = new Vector2(1f, 1f);

        internal override void UpdateCore(Context context)
        {
            if (Layer is null)
                return;

            if (renderLayer.RenderView != RenderView)
                renderLayer.RenderView = RenderView;

            if  (renderLayer.Layer != Layer)
                renderLayer.Layer = Layer;

            if (context is StrideContext strideContext)
            {
                if (ImGui.BeginChild("##RenderWidget", Size.FromHectoToImGui(), ImGuiChildFlags.None, ImGuiWindowFlags.ChildWindow))
                {
                    var pos = ImGui.GetWindowPos();
                    var size = ImGui.GetWindowSize();
                    renderLayer.Viewport = new Viewport(pos.X, pos.Y, size.X, size.Y);
                    var drawList = ImGui.GetWindowDrawList();
                    drawList.AddCallback(GCHandle.ToIntPtr(renderLayerHandle), IntPtr.Zero);
                }
                ImGui.EndChild();
            }
        }

        public void Dispose()
        {
            renderLayerHandle.Free();
        }
    }
}
