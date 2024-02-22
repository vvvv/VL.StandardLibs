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
    public /*unsafe*/ sealed partial class RenderWidget : PrimitiveWidget, IDisposable
    {
        private readonly RenderLayer renderLayer;

        //public delegate void RenderDrawCallback(RenderDrawContext* context, ImDrawListPtr parentList, ImDrawCmdPtr drawCmd);

        //// FunctionPointer
        //delegate* <RenderDrawContext*, ImDrawListPtr, ImDrawCmdPtr, void> callback;

        //// Function
        //private static void DrawCore(RenderDrawContext* context, ImDrawListPtr parentList, ImDrawCmdPtr drawCmd)
        //{
        //}


        public RenderWidget()
        {
            renderLayer = new RenderLayer();
        }

        public IGraphicsRendererBase? Layer { private get; set ; }

        public RenderView? RenderView { private get; set; } = new RenderView();

        public Vector2 Size { private get; set; } = new Vector2(1f, 1f);

        protected override void Draw(Context context, in ImDrawListPtr drawList, in System.Numerics.Vector2 offset)
        {
            if (Layer is null)
                return;

            if (renderLayer.RenderView != RenderView)
                renderLayer.RenderView = RenderView;

            if  (renderLayer.Layer != Layer)
                renderLayer.Layer = Layer;

            if (context is StrideContext strideContext)
            {
                var size     = Size.FromHectoToImGui();
                var position = ImGui.GetCursorPos();

                var sPos = ImGui.GetCursorScreenPos();

                var si = ImGui.GetItemRectSize();

                renderLayer.Viewport = new Viewport(sPos.X, sPos.Y, size.X, size.Y);

                int id = strideContext.AddLayer(renderLayer);
                drawList.AddCallback((IntPtr)id, IntPtr.Zero);

                ImGui.SetCursorPosY(position.Y + size.Y);


            }

            ////Unsafe Version
            //fixed (RenderLayer* ptr = &renderLayer)
            //{
            //    drawList.AddCallback((IntPtr)ptr, IntPtr.Zero);
            //}

            // Unsave CallBack Sample
            //fixed (RenderLayer* ptr = &renderLayer)
            //{
            //    callback = &DrawCore;
            //    drawList.AddCallback(callback, IntPtr.Zero);
            //}
        }

        public void Dispose()
        {

        }
    }
}
