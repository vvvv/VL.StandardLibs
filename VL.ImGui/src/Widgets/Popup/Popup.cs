using Stride.Core.Mathematics;
using VL.Lib.Reactive;

namespace VL.ImGui.Widgets
{
    using ImGui = ImGuiNET.ImGui;

    [GenerateNode(Category = "ImGui.Widgets", GenerateImmediate = false)]
    public sealed partial class Popup : Widget
    {
        public Widget? Content { get; set; }

        public string? Label { get; set; }

        /// <summary>
        /// Position of the Popup.
        /// </summary>
        public IChannel<Vector2>? Position { private get; set; }
        ChannelFlange<Vector2> PositionFlange = new ChannelFlange<Vector2>(new Vector2());

        /// <summary>
        /// Returns true if the Popup is open. Set to true to open the Popup.
        /// </summary>
        public IChannel<bool>? Visible { private get; set; }
        ChannelFlange<bool> VisibleFlange = new ChannelFlange<bool>(false);

        /// <summary>
        /// Returns true if content is visible. 
        /// </summary>
        public bool ContentIsVisible { get; private set; } = false;

        public ImGuiNET.ImGuiWindowFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var position = PositionFlange.Update(Position, out bool positionChanged);
            var visible = VisibleFlange.Update(Visible, out bool visibilityChanged);
            var label = widgetLabel.Update(Label);
            ContentIsVisible = false;

            if (visibilityChanged)
            {
                if (visible)
                    ImGui.OpenPopup(label);
                else
                    ImGui.CloseCurrentPopup();
            }

            if (positionChanged || visibilityChanged)
            {
                ImGui.SetNextWindowPos(position.FromHectoToImGui());
            }

            if (visible)
            {

                ContentIsVisible = ImGui.BeginPopup(label);
                VisibleFlange.Value = ContentIsVisible;

                if (ContentIsVisible)
                {
                    try
                    {
                        context.Update(Content);
                        PositionFlange.Value = ImGui.GetWindowPos().ToVLHecto();
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
