using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Queries")]
    internal partial class GetMousePos : Widget
    {
        public Vector2 Value { get; private set; }

        protected override void UpdateCore(Context context)
        {
            var value = ImGuiNET.ImGui.GetMousePos();
            Value = value.ToVLHecto();
        }
    }
}
