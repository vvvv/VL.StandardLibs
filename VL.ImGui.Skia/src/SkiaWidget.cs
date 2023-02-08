using ImGuiNET;
using SkiaSharp;
using Stride.Core.Mathematics;
using VL.ImGui.Widgets.Primitives;
using VL.Skia;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets.Internal", IsStylable = false)]
    public sealed partial class SkiaWidget : PrimitiveWidget
    {
        public ILayer? Layer { private get; set; }

        public Vector2 Size { private get; set; } = new Vector2(1f, 1f);

        protected override void Draw(Context context, in ImDrawListPtr drawList, in System.Numerics.Vector2 offset)
        {
            if (context is SkiaContext skiaContext)
            {
                var _ = Size.FromHectoToImGui();
                if (Layer != null)
                    skiaContext.AddLayer(new Vector2(_.X, _.Y), Layer);
            }
        }
    }
}
