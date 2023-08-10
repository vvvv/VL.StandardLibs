namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Lock horizontal starting position. Capture the whole group bounding box into one "item" (so you can use IsItemHovered or layout primitives such as SameLine on whole group, etc.)
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateImmediate = false)]
    internal partial class Group : Widget
    {
        public Widget? Input { get; set; }

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.BeginGroup();
            try
            {
                context.Update(Input);
            }
            finally
            {
                ImGuiNET.ImGui.EndGroup();
            } 
        }
    }
}
