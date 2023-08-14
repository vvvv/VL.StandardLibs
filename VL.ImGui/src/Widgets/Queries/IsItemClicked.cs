using ImGuiNET;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Is the last item hovered and mouse clicked on?
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class IsItemClicked : Query
    {

        public ImGuiMouseButton Flags { private get; set; }

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            var capturedState = context.CapturedItemState;
            if (capturedState.HasValue)
                Value = Flags switch
                {
                    ImGuiMouseButton.Left => capturedState.Value.IsLeftClicked,
                    ImGuiMouseButton.Middle => capturedState.Value.IsMiddleClicked,
                    ImGuiMouseButton.Right => capturedState.Value.IsRightClicked,
                    _ => false
                };
            else
                Value = ImGuiNET.ImGui.IsItemClicked(Flags);
        }
    }
}