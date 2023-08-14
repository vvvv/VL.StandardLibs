using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Get scrolling amount [0 .. GetScrollMax]
    /// </summary>

    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class GetScroll : Query
    {

        public Vector2 Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            var x = ImGuiNET.ImGui.GetScrollX().ToVLHecto();
            var y = ImGuiNET.ImGui.GetScrollY().ToVLHecto();
            Value = new Vector2(x, y);
        }
    }
}
