using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Retrieve available space from a given point. == GetContentRegionMax() - GetCursorPos()
    /// </summary>

    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class GetContentRegionAvail : Query
    {

        public Vector2 Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            var size = ImGuiNET.ImGui.GetContentRegionAvail();
            Value = ImGuiConversion.ToVLHecto(size);
        }
    }
}
