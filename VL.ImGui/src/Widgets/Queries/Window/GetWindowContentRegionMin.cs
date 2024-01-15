using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Content boundaries min for the full window (roughly (0,0)-Scroll), in window coordinates
    /// </summary>

    [GenerateNode(Category = "ImGui.Queries")]
    internal partial class GetWindowContentRegionMin : Query
    {

        public Vector2 Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            var size = ImGuiNET.ImGui.GetWindowContentRegionMin();
            Value = ImGuiConversion.ToVLHecto(size);
        }
    }
}
