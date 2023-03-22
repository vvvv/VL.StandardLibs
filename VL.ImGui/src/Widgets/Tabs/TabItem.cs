using VL.Lib.Reactive;
using System.Reactive;

namespace VL.ImGui.Widgets
{

    [GenerateNode(Category = "ImGui.Widgets", GenerateImmediate = false)]
    internal sealed partial class TabItem : Widget
    {
        public Widget? Content { get; set; }

        public string? Label { get; set; }

        /// <summary>
        /// If set the Tab will have a close button which will push to the channel once clicked.
        /// </summary>
        public Channel<Unit> Closing { get; set; } = DummyChannel<Unit>.Instance;

        /// <summary>
        /// Returns true if the Tab is displayed. Set to true to display the Tab.
        /// </summary>
        public Channel<bool>? Visible { private get; set; }
        ChannelFlange<bool> VisibleFlange = new ChannelFlange<bool>(true);
        /// <summary>
        /// Returns true if the Tab is displayed.
        /// </summary>
        public bool _IsVisible => VisibleFlange.Value;

        /// <summary>
        /// Returns true if the Tab is activated/selected. Set to true to activate the Tab.
        /// </summary>
        public Channel<bool>? Active { private get; set; }
        ChannelFlange<bool> ActiveFlange = new ChannelFlange<bool>(false);
        /// <summary>
        /// Returns true if the Tab is activated/selected. 
        /// </summary>
        public bool _IsActive => ActiveFlange.Value;


        public ImGuiNET.ImGuiTabItemFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var isVisible = VisibleFlange.Update(Visible);
            var isActive = ActiveFlange.Update(Active, out var activateHasChanged);
            var gotActivated = activateHasChanged && isActive;
            var flags = Flags;

            if (isVisible)
            {
                if (gotActivated)
                    flags |= ImGuiNET.ImGuiTabItemFlags.SetSelected;

                if (Closing.IsValid() || flags != ImGuiNET.ImGuiTabItemFlags.None)
                {
                    isActive = ImGuiNET.ImGui.BeginTabItem(Context.GetLabel(this, Label), ref isVisible, flags);
                    if (!isVisible)
                        Closing.Value = default;
                }
                else
                    isActive = ImGuiNET.ImGui.BeginTabItem(Context.GetLabel(this, Label));

                ActiveFlange.Value = isActive;

                if (isActive)
                {
                    try
                    {
                        context.Update(Content);
                    }
                    finally
                    {
                        ImGuiNET.ImGui.EndTabItem();
                    }
                }
            }
        }
    }
}
