using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets", IsStylable = false)]
    internal partial class Dummy : Widget
    {
        public Vector2 Size { private get; set; }

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.Dummy(Size.FromHectoToImGui());
        }
    }
}
