using Stride.Core.Mathematics;
using VL.Lib.Reactive;
using System.Reactive;

namespace VL.ImGui.Widgets
{
    using ImGui = ImGuiNET.ImGui;

    /// <summary>
    /// Modal Popups block normal mouse hovering detection (and therefore most mouse interactions) behind them. They can't be closed by clicking outside of them.
    /// </summary>
    [GenerateNode(Category = "ImGui.Widgets", GenerateImmediate = false)]
    public sealed partial class Modal : Widget
    {

        public Widget? Content { get; set; }

        public string? Label { get; set; }

        /// <summary>
        /// If set the Popup will have a close button which will push to the channel once clicked.
        /// </summary>
        public IChannel<Unit> Closing { get; set; } = ChannelHelpers.Dummy<Unit>();

        /// <summary>
        /// Bounds of the Window.
        /// </summary>
        public IChannel<RectangleF>? Bounds { private get; set; }
        ChannelFlange<RectangleF> BoundsFlange = new ChannelFlange<RectangleF>(new RectangleF(0f, 0f, 1f, 1f));

        /// <summary>
        /// Returns true if the Modal Window is open. Set to true to open the Modal Window.
        /// </summary>
        public IChannel<bool>? Visible { private get; set; }
        ChannelFlange<bool> VisibleFlange = new ChannelFlange<bool>(false);
        
        /// <summary>
        /// Returns true if content is visible.
        /// </summary>
        public bool ContentIsVisible { get; private set; } = false;

        /// <summary>
        /// Returns true if close button is clicked. 
        /// </summary>
        public bool CloseClicked { get; private set; } = false;

        public ImGuiNET.ImGuiWindowFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var visible = VisibleFlange.Update(Visible, out bool visibilityChanged);
            var bounds = BoundsFlange.Update(Bounds, out bool boundsChanged);
            var label = widgetLabel.Update(Label);
            CloseClicked = false;
            ContentIsVisible = false;

            if (visibilityChanged)
            {
                if (visible)
                    ImGui.OpenPopup(label);
                else
                    ImGui.CloseCurrentPopup();
            }

            if (boundsChanged || visibilityChanged)
            {
                ImGui.SetNextWindowPos(bounds.TopLeft.FromHectoToImGui());
                ImGui.SetNextWindowSize(bounds.Size.FromHectoToImGui());
            }

            if (visible)
            {
                if (Closing.IsValid())
                {
                    // From Imgui Demo:
                    // ...Also demonstrate passing a bool* to BeginPopupModal(), this will create a regular close button which
                    // will close the popup. Note that the visibility state of popups is owned by imgui, so the input value
                    // of the bool actually doesn't matter here.
                    // https://github.com/ocornut/imgui/blob/2d38bc99b3b0013952d3d390397297083b767972/imgui_demo.cpp#L3535

                    var isVisible = true;
                    ContentIsVisible = ImGui.BeginPopupModal(label, ref isVisible, Flags);
                    if (!isVisible)
                    {
                        Closing.Value = default;
                        CloseClicked = true;
                    }
                        
                }
                else
                {
                    ContentIsVisible = ImGui.BeginPopupModal(label);
                }

                VisibleFlange.Value = ContentIsVisible;

                if (ContentIsVisible)
                {
                    try
                    {
                        context.Update(Content);

                        var pos = ImGui.GetWindowPos().ToVLHecto();
                        var size = ImGui.GetWindowSize().ToVLHecto();
                        BoundsFlange.Value = new RectangleF(pos.X, pos.Y, size.X, size.Y);
                    }
                    finally
                    {
                        ImGui.EndPopup();
                    }
                }
            }
        }
    }
}
