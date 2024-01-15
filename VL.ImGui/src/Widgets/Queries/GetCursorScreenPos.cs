using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Initial cursor position in window coordinates
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class GetCursorStartPos : Query
    {
        public Vector2 Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            var pos = ImGuiNET.ImGui.GetCursorStartPos();
            Value = ImGuiConversion.ToVLHecto(pos);
        }
    }
}
