using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class CalcTextSize : Query
    {

        public Vector2 Value { get; private set; }

        public string? Text { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var width = ImGuiNET.ImGui.CalcTextSize(Text ?? string.Empty);
            Value = ImGuiConversion.ToVLHecto(width);
        }
    }
}
