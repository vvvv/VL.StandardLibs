using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Cursor position in window coordinates (relative to window position)
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class SetCursorPosition : Widget
    {
        public Vector2 Position { private get; set; }

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.SetCursorPos(Position.FromHectoToImGui());
        }
    }
}
