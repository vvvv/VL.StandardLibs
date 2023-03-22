using VL.Lib.Reactive;
using System.Reactive;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets", Tags = "tree", GenerateImmediate = false)]
    internal sealed partial class CollapsingHeader : Widget
    {
        public Widget? Content { get; set; }

        public string? Label { get; set; }

        /// <summary>
        /// If set the Header will have a close button which will push to the channel once clicked.
        /// </summary>
        public Channel<Unit> Closing { get; set; } = DummyChannel<Unit>.Instance;

        /// <summary>
        /// Returns true if the Header is displayed. Set to true to display the Header.
        /// </summary>
        public Channel<bool>? Visible { private get; set; }
        ChannelFlange<bool> VisibleFlange = new ChannelFlange<bool>(true);
        /// <summary>
        /// Returns true if the Header is displayed.
        /// </summary>
        public bool _IsVisible => VisibleFlange.Value;

        /// <summary>
        /// Returns true if the Header is opened (not collapsed). Set to true to open the Header.
        /// </summary>
        public Channel<bool>? Open { private get; set; }
        ChannelFlange<bool> OpenFlange = new ChannelFlange<bool>(false);

        /// <summary>
        /// Returns true if the Header is opened (visible and not collapsed). 
        /// </summary>
        public bool _IsOpen => OpenFlange.Value;

        public ImGuiNET.ImGuiTreeNodeFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var isVisible = VisibleFlange.Update(Visible);

            if (isVisible)
            {
                var isOpen= OpenFlange.Update(Open);
                ImGuiNET.ImGui.SetNextItemOpen(isOpen);

                if (Closing.IsValid())
                {
                    var visible = true;
                    isOpen = ImGuiNET.ImGui.CollapsingHeader(Context.GetLabel(this, Label), ref visible, Flags);
                    if (!visible)
                        Closing.Value = default;
                }
                else
                    isOpen = ImGuiNET.ImGui.CollapsingHeader(Context.GetLabel(this, Label), Flags);

                OpenFlange.Value = isOpen;

                if (isOpen)
                {
                    context?.Update(Content);
                }
            }
        }
    }
}
