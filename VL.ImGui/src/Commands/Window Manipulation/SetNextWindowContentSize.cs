using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class SetNextWindowContentSize : Widget
    {
        public Vector2 Size { private get; set; }

        public bool Enabled { private get; set; } = true;

        internal override void UpdateCore(Context context)
        {
            if (Enabled)
                ImGuiNET.ImGui.SetNextWindowContentSize (Size.FromHectoToImGui());
        }
    }
}
