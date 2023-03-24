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
        public Channel<bool>? Collapsed { private get; set; }
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

            ContentIsVisible = ImGuiNET.ImGui.TreeNodeEx(Context.GetLabel(this, Label), Flags);

            CollapsedFlange.Value = !ContentIsVisible;

            if (ContentIsVisible)
            {
                context?.Update(Content);
                ImGuiNET.ImGui.TreePop();
            }
        }
    }
}
