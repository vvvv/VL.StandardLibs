using ImGuiNET;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Viewport = Stride.Graphics.Viewport;

namespace VL.ImGui.Widgets
{
    using ImGui = ImGuiNET.ImGui;

    [GenerateNode(Category = "ImGui.Widgets.Internal", IsStylable = false)]
    public sealed partial class RenderWidget : Widget, IDisposable
    {
        private readonly RenderLayer renderLayer;
        private IContextWithRenderer? strideContext;


        public RenderWidget()
        {
            renderLayer = new RenderLayer();
        }

        public IGraphicsRendererBase? Layer { private get; set ; }

        public RenderView? RenderView { private get; set; } = new RenderView();

        public Vector2 Size { private get; set; } = new Vector2(1f, 1f);

        internal override void UpdateCore(Context context)
        {
            if (context is StrideContext strideContext)
            {
                this.strideContext = strideContext;

                if (Layer is null)
                {
                    strideContext.RemoveRenderer(renderLayer);
                    return;
                }
                

                if (renderLayer.RenderView != RenderView)
                    renderLayer.RenderView = RenderView;

                if  (renderLayer.Layer != Layer)
                    renderLayer.Layer = Layer;

            
                if (ImGui.BeginChild("##RenderWidget", Size.FromHectoToImGui(), ImGuiChildFlags.None, ImGuiWindowFlags.ChildWindow))
                {
                    var pos = ImGui.GetWindowPos();
                    var size = ImGui.GetWindowSize();
                    renderLayer.Viewport = new Viewport(pos.X, pos.Y, size.X, size.Y);
                    var drawList = ImGui.GetWindowDrawList();
                    drawList.AddCallback(strideContext.AddRenderer(renderLayer), IntPtr.Zero);
                }
                ImGui.EndChild();
            }
        }

        public void Dispose()
        {
            strideContext?.RemoveRenderer(renderLayer);
        }
    }
}
