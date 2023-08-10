using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Get upper-left bounding rectangle of the last item (screen space)
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class GetItemRectSize : Query
    {

        public Vector2 Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            var size = ImGuiNET.ImGui.GetItemRectSize();
            Value = ImGuiConversion.ToVLHecto(size);
        }
    }
}
