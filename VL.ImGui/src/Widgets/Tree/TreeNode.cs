using VL.Lib.Reactive;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets", Tags = "tree", GenerateImmediate = false)]
    internal sealed partial class TreeNode : Widget
    {
        public Widget? Content { get; set; }

        public string? Label { get; set; }

        /// <summary>
        /// Returns true if the TreeNode is open (not collapsed). Set to true to open the TreeNode.
        /// </summary>
        public Channel<bool>? IsOpen { private get; set; }
        ChannelFlange<bool> IsOpenFlange = new ChannelFlange<bool>(false);
        /// <summary>
        /// Returns true if the TreeNode is open (not collapsed). 
        /// </summary>
        public bool _IsOpen => IsOpenFlange.Value;

        public ImGuiNET.ImGuiTreeNodeFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var isOpen = IsOpenFlange.Update(IsOpen);

            ImGuiNET.ImGui.SetNextItemOpen(isOpen);

            isOpen = ImGuiNET.ImGui.TreeNodeEx(Context.GetLabel(this, Label), Flags);

            IsOpenFlange.Value = isOpen;

            if (isOpen)
            {
                context?.Update(Content);
                ImGuiNET.ImGui.TreePop();
            }
        }
    }
}
