using ImGuiNET;

namespace VL.ImGui.Widgets
{
    // it has this begin/end pattern. So offer as region?!
    // user shall call SetDragDropPayload inside the region. for now going for a simple node. (DragDrop)

    //[GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class DragDropSource : Widget
    {
        public ImGuiNET.ImGuiDragDropFlags Flags { private get; set; }

        /// <summary>
        /// call SetDragDropPayload somewhere in here
        /// </summary>
        public Widget? Content { get; set; }

        internal override void UpdateCore(Context context)
        {               
            if (ImGuiNET.ImGui.BeginDragDropSource(Flags))
            {
                using (context.CaptureItemState())
                {
                    context.Update(Content);
                    ImGuiNET.ImGui.EndDragDropSource();
                }
            }
        }
    }
}
