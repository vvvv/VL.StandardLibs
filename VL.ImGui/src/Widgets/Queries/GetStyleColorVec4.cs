using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Retrieve style color as stored in ImGuiStyle structure.
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries")]
    internal partial class GetStyleColor : Query
    {
        public ImGuiNET.ImGuiCol Flag { private get; set; }

        public Color4 Value { get; private set; }

        internal override unsafe void UpdateCore(Context context)
        {
            var color = ImGuiNET.ImGui.GetStyleColorVec4(Flag);
            Value = ImGuiConversion.ToVLColor4(*color);
        }
    }
}
