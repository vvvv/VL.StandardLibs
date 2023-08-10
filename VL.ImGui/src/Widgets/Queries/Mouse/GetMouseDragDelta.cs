using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Return the delta from the initial clicking position while the mouse button is pressed or was just released. This is locked and return 0.0f until the mouse moves past a distance threshold at least once (if lock_threshold &lt; -1.0f, uses io.MouseDraggingThreshold)
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries")]
    internal partial class GetMouseDragDelta : Query
    {
        public ImGuiNET.ImGuiMouseButton Flags { private get; set; }

        public float Threshold { private get; set; } = -2.0f;

        public Vector2 Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            var value = ImGuiNET.ImGui.GetMouseDragDelta(Flags, Threshold);
            Value = value.ToVLHecto();
        }
    }
}
