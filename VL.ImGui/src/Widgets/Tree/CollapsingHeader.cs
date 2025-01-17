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
        public IChannel<Unit> Closing { get; set; } = ChannelHelpers.Dummy<Unit>();

        /// <summary>
        /// Returns true if the Header is displayed. Set to true to display the Header.
        /// </summary>
        [Pin(DefaultValue = "true")]
        public IChannel<bool>? Visible { private get; set; }
        ChannelFlange<bool> VisibleFlange = new ChannelFlange<bool>(true);

        /// <summary>
        /// Returns true if the Header is collapsed. Set to true to collapse the Header.
        /// </summary>
        public IChannel<bool>? Collapsed { private get; set; }
        ChannelFlange<bool> CollapsedFlange = new ChannelFlange<bool>(false);

        /// <summary>
        /// Returns true if content is visible. 
        /// </summary>
        public bool ContentIsVisible { get; private set; } = false;

        /// <summary>
        /// Returns true if close button is clicked. 
        /// </summary>
        public bool CloseClicked { get; private set; } = false;

        public ImGuiNET.ImGuiTreeNodeFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var visible = VisibleFlange.Update(Visible);
            CloseClicked = false;
            ContentIsVisible = false;

            if (visible)
            {
                var collapsed = CollapsedFlange.Update(Collapsed);
                ImGuiNET.ImGui.SetNextItemOpen(!collapsed);

                if (Closing.IsValid())
                {
                    var isVisible = true;
                    ContentIsVisible = ImGuiNET.ImGui.CollapsingHeader(widgetLabel.Update(Label), ref isVisible, Flags);
                    if (!isVisible)
                    {
                        Closing.Value = default;
                        CloseClicked = true;
                    }      
                }
                else
                    ContentIsVisible = ImGuiNET.ImGui.CollapsingHeader(widgetLabel.Update(Label), Flags);

                CollapsedFlange.Value = !ContentIsVisible;

                if (ContentIsVisible)
                {
                    // ImGui keeps track of the LAST item state only. We therefor need to keep track of those states on our own when we build up item stacks.
                    using (context.CaptureItemState())
                    {
                        // Captured state is NOT set
                        context.Update(Content);
                    }
                    // Captured state is set for subsequent queries and will be unset by non-query widgets.
                }
            }
        }
    }
}
