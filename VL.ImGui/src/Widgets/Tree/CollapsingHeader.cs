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
        /// Returns true if the Header is collapsed. Set to true to collapse the Header.
        /// </summary>
        public Channel<bool>? Collapsed { private get; set; }
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
                    ContentIsVisible = ImGuiNET.ImGui.CollapsingHeader(Context.GetLabel(this, Label), ref isVisible, Flags);
                    if (!isVisible)
                    {
                        Closing.Value = default;
                        CloseClicked = true;
                    }      
                }
                else
                    ContentIsVisible = ImGuiNET.ImGui.CollapsingHeader(Context.GetLabel(this, Label), Flags);

                CollapsedFlange.Value = !ContentIsVisible;

                if (ContentIsVisible)
                {
                    context?.Update(Content);
                }
            }
        }
    }
}
