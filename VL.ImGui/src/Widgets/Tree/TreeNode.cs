using VL.Lib.Reactive;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets", Tags = "tree", GenerateImmediate = false)]
    internal sealed partial class TreeNode : Widget
    {
        public Widget? Content { get; set; }

        public string? Label { get; set; }

        /// <summary>
        /// Returns true if the TreeNode is opened. Set to true to open the TreeNode.
        /// </summary>
        public Channel<bool>? Open { private get; set; }
        ChannelFlange<bool> OpenFlange = new ChannelFlange<bool>(true);
        /// <summary>
        /// Returns true if the TreeNode is opened. 
        /// </summary>
        public bool _IsOpen => OpenFlange.Value;

        public ImGuiNET.ImGuiTreeNodeFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var isOpen = OpenFlange.Update(Open);

            ImGuiNET.ImGui.SetNextItemOpen(isOpen);

            isOpen = ImGuiNET.ImGui.TreeNodeEx(Context.GetLabel(this, Label), Flags);

            OpenFlange.Value = isOpen;

            if (isOpen)
            {
                context?.Update(Content);
                ImGuiNET.ImGui.TreePop();
            }
        }
    }
}
