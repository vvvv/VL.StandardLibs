using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Get current window size
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries")]
    internal partial class GetWindowSize : Query
    {

        public Vector2 Value { get; private set; }


        internal override void UpdateCore(Context context)
        {
            var size = ImGuiNET.ImGui.GetWindowSize();
            Value = ImGuiConversion.ToVLHecto(size);
        }
    }
}
