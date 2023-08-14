using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Get current window position in screen space (useful if you want to do your own drawing via the DrawList API)
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries")]
    internal partial class GetWindowPos : Query
    {

        public Vector2 Value { get; private set; }


        internal override void UpdateCore(Context context)
        {
            var position = ImGuiNET.ImGui.GetWindowPos();
            Value = ImGuiConversion.ToVLHecto(position);
        }
    }
}
