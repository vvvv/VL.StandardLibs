using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Queries")]
    internal partial class GetMousePos : Query
    {
        public Vector2 Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            var value = ImGuiNET.ImGui.GetMousePos();
            Value = value.ToVLHecto();
        }
    }
}
