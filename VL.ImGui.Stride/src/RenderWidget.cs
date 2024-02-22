using ImGuiNET;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using VL.ImGui.Widgets.Primitives;
using VL.Lib.Mathematics;
using VL.Skia;
using VL.Stride.Rendering;
using Viewport = Stride.Graphics.Viewport;

namespace VL.ImGui.Widgets
{
    using ImGui = ImGuiNET.ImGui;

    [GenerateNode(Category = "ImGui.Widgets.Internal", IsStylable = false)]
    public sealed partial class RenderWidget : Widget
    {
        private readonly RenderLayer renderLayer;

        public RenderWidget()
        {
            renderLayer = new RenderLayer();
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
                    #pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
                    unsafe
                    {
                        fixed (RenderLayer* ptr = &renderLayer)
                        {
                            var drawList = ImGui.GetWindowDrawList();
                            drawList.AddCallback((IntPtr)ptr, IntPtr.Zero);
                        }
                    }
                    #pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
                }
                ImGui.EndChild();
            }
        }

        //public delegate void RenderDrawCallback(RenderDrawContext* context, ImDrawListPtr parentList, ImDrawCmdPtr drawCmd);

        //// FunctionPointer
        //delegate* <RenderDrawContext*, ImDrawListPtr, ImDrawCmdPtr, void> callback;

        //// Function
        //private static void DrawCore(RenderDrawContext* context, ImDrawListPtr parentList, ImDrawCmdPtr drawCmd)
        //{
        //}
    }
}
