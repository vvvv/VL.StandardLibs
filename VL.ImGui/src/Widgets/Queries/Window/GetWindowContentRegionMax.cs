using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Content boundaries max for the full window (roughly (0,0)+Size-Scroll) where Size can be override with SetNextWindowContentSize(), in window coordinates
    /// </summary>

    [GenerateNode(Category = "ImGui.Queries")]
    internal partial class GetWindowContentRegionMax : Query
    {

        public Vector2 Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            var size = ImGuiNET.ImGui.GetWindowContentRegionMax();
            Value = ImGuiConversion.ToVLHecto(size);
        }
    }
}
