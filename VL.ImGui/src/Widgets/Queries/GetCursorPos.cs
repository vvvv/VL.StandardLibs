using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Cursor position in window coordinates (relative to window position)
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class GetCursorPos : Query
    {
        public Vector2 Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            var pos = ImGuiNET.ImGui.GetCursorPos();
            Value = ImGuiConversion.ToVLHecto(pos);
        }
    }
}
