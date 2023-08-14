using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Get maximum scrolling amount ~~ ContentSize - WindowSize - DecorationsSize
    /// </summary>

    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class GetScrollMax : Query
    {

        public Vector2 Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            var x = ImGuiNET.ImGui.GetScrollMaxX().ToVLHecto();
            var y = ImGuiNET.ImGui.GetScrollMaxY().ToVLHecto();
            Value = new Vector2(x, y);
        }
    }
}
