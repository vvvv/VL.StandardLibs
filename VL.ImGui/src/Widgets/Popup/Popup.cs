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
        public Channel<Vector2>? Position { private get; set; }
        ChannelFlange<Vector2> PositionFlange = new ChannelFlange<Vector2>(new Vector2());

        /// <summary>
        /// Returns true if the Popup is open. Set to true to open the Popup.
        /// </summary>
        public Channel<bool>? Open { private get; set; }
        ChannelFlange<bool> OpenFlange = new ChannelFlange<bool>(false);

        /// <summary>
        /// Returns true if the Popup is open. 
        /// </summary>
        public bool _IsVisible => OpenFlange.Value;

        public ImGuiNET.ImGuiWindowFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var position = PositionFlange.Update(Position, out bool positionChanged);
            var isOpen = OpenFlange.Update(Open, out bool visibilityChanged);
            var label = Context.GetLabel(this, Label);

            if (visibilityChanged)
            {
                if (isOpen)
                    ImGui.OpenPopup(label);
                else
                    ImGui.CloseCurrentPopup();
            }

            if (isOpen)
            {
                if (positionChanged || visibilityChanged)
                {
                    ImGui.SetNextWindowPos(position.FromHectoToImGui());
                }

                isOpen = ImGui.BeginPopup(label);

                OpenFlange.Value = isOpen;
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
