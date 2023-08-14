using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Get lower-right bounding rectangle of the last item (screen space)
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class GetItemRectMax: Query
    {

        public Vector2 Value { get; private set; }


        internal override void UpdateCore(Context context)
        {
            var size = ImGuiNET.ImGui.GetItemRectMax();
            Value = ImGuiConversion.ToVLHecto(size);
        }
    }
}
