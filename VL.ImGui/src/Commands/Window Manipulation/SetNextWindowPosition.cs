using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    //TODO: This functionality is part of the window. Remove the Command?
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class SetNextWindowPosition : Widget
    {
        public Vector2 Position { private get; set; }

        public bool Enabled { private get; set; } = true;

        internal override void UpdateCore(Context context)
        {
            if (Enabled)
                ImGuiNET.ImGui.SetNextWindowPos(Position.FromHectoToImGui());
        }
    }
}
