using VL.Lib.Reactive;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets", Tags = "tree", GenerateImmediate = false)]
    internal sealed partial class CollapsingHeader : Widget
    {
        public Widget? Content { get; set; }

        public string? Label { get; set; }

        /// <summary>
        /// Display an additional small close button on upper right of the header
        /// </summary>
        public bool HasCloseButton { get; set; } = true;

        /// <summary>
        /// Returns true if the header is displayed. Set to true to display the header.
        /// </summary>
        public Channel<bool>? IsVisible { private get; set; } 
        ChannelFlange<bool> IsVisibleFlange = new ChannelFlange<bool>(true);
        /// <summary>
        /// Returns true if the header is displayed.
        /// </summary>
        public bool _IsVisible => IsVisibleFlange.Value;

        /// <summary>
        /// Returns true if the Header is open (not collapsed). Set to true to open the header.
        /// </summary>
        public Channel<bool>? IsOpen { private get; set; }
        ChannelFlange<bool> IsOpenFlange = new ChannelFlange<bool>(false);
        /// <summary>
        /// Returns true if the Header is open (not collapsed). 
        /// </summary>
        public bool _IsOpen => IsOpenFlange.Value;

        public ImGuiNET.ImGuiTreeNodeFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var isVisible = IsVisibleFlange.Update(IsVisible);
            var isOpen = IsOpenFlange.Update(IsOpen);

            ImGuiNET.ImGui.SetNextItemOpen(isOpen);

            if (HasCloseButton)
            {
                isOpen = ImGuiNET.ImGui.CollapsingHeader(Context.GetLabel(this, Label), ref isVisible, Flags);
                IsVisibleFlange.Value = isVisible; // close button might have been pressed
            }
            else
                isOpen = ImGuiNET.ImGui.CollapsingHeader(Context.GetLabel(this, Label), Flags);

            IsOpenFlange.Value = isOpen;

            if (isOpen)
            {
                context?.Update(Content);
            }
        }
    }
}
