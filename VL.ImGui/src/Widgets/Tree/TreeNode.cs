using VL.Lib.Reactive;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets", Tags = "tree", GenerateImmediate = false)]
    internal sealed partial class TreeNode : Widget
    {
        public Widget? Content { get; set; }

        public string? Label { get; set; }

        /// <summary>
        /// Returns true if the TreeNode is collapsed. Set to true to collapse the TreeNode.
        /// </summary>
        [Pin(DefaultValue = "true")]
        public IChannel<bool>? Collapsed { private get; set; }
        ChannelFlange<bool> CollapsedFlange = new ChannelFlange<bool>(true);

        /// <summary>
        /// Returns true if content is visible. 
        /// </summary>
        public bool ContentIsVisible { get; private set; } = false;

        public ImGuiNET.ImGuiTreeNodeFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var collapsed = CollapsedFlange.Update(Collapsed);

            ImGuiNET.ImGui.SetNextItemOpen(!collapsed);

            ContentIsVisible = ImGuiNET.ImGui.TreeNodeEx(widgetLabel.Update(Label), Flags);

            CollapsedFlange.Value = !ContentIsVisible;

            if (ContentIsVisible)
            {
                // ImGui keeps track of the LAST item state only. We therefor need to keep track of those states on our own when we build up item stacks.
                using (context.CaptureItemState())
                {
                    // Captured state is NOT set
                    context.Update(Content);
                    ImGuiNET.ImGui.TreePop();
                }
                // Captured state is set for subsequent queries and will be unset by non-query widgets.
            }
        }
    }
}
