using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Is mouse hovering given bounding rect (in screen space). Clipped by current clipping settings, but disregarding of other consideration of focus/window ordering/popup-block.
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries")]
    internal partial class IsMouseHoveringRect : Query
    {

        public RectangleF Rectangle { private get; set; }

        public bool Clip { private get; set; }

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsMouseHoveringRect(Rectangle.TopLeft.FromHectoToImGui(), Rectangle.BottomRight.FromHectoToImGui(), Clip);
        }
    }
}
