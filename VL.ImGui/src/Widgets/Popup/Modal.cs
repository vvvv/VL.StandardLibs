using Stride.Core.Mathematics;
using VL.Lib.Reactive;

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
        /// Display a regular close button.
        /// </summary>
        public bool HasCloseButton { get; set; } = true;

        /// <summary>
        /// Bounds of the Window.
        /// </summary>
        public Channel<RectangleF>? Bounds { private get; set; }
        ChannelFlange<RectangleF> BoundsFlange = new ChannelFlange<RectangleF>(new RectangleF(0f, 0f, 1f, 1f));

        /// <summary>
        /// Returns true if the Modal Window is open. Set to true to open the Modal Window.
        /// </summary>
        public Channel<bool>? IsOpen { private get; set; }
        ChannelFlange<bool> IsOpenFlange = new ChannelFlange<bool>(false);
        /// <summary>
        /// Returns true if the Modal Window is open. 
        /// </summary>
        public bool _IsOpen => IsOpenFlange.Value;

        public ImGuiNET.ImGuiWindowFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var bounds = BoundsFlange.Update(Bounds, out bool boundsChanged);
            var isOpen = IsOpenFlange.Update(IsOpen, out bool hasChanged);
            var label = Context.GetLabel(this, Label);

            if (boundsChanged)
            {
                ImGui.SetNextWindowPos(bounds.TopLeft.FromHectoToImGui());
                ImGui.SetNextWindowSize(bounds.Size.FromHectoToImGui());
            }

            if (hasChanged)
            {
                if (isOpen)
                    ImGui.OpenPopup(label);
                else
                    ImGui.CloseCurrentPopup();
            }

            if (isOpen)
            {        
                if (HasCloseButton)
                {
                    // From Imgui Demo:
                    // ...Also demonstrate passing a bool* to BeginPopupModal(), this will create a regular close button which
                    // will close the popup. Note that the visibility state of popups is owned by imgui, so the input value
                    // of the bool actually doesn't matter here.
                    // https://github.com/ocornut/imgui/blob/2d38bc99b3b0013952d3d390397297083b767972/imgui_demo.cpp

                    var unusedOpen = true;
                    isOpen = ImGui.BeginPopupModal(label, ref unusedOpen, Flags);
                }
                else
                {
                    isOpen = ImGui.BeginPopupModal(label);
                }

                IsOpenFlange.Value = isOpen;
                if (isOpen)
                {
                    try
                    {
                        context?.Update(Content);
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
