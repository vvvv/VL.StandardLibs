using Stride.Core.Mathematics;
using ImGuiNET;
using VL.Lib.Reactive;

namespace VL.ImGui.Windows
{
    using ImGui = ImGuiNET.ImGui;

    [GenerateNode(Category = "ImGui.Widgets.Internal", GenerateRetained = false)]
    internal sealed partial class WindowCore : Widget
    {
        public Widget? Content { get; set; }

        public string Name { get; set; } = "Window";

        public bool HasCloseButton { get; set; } = true;

        /// <summary>
        /// Bounds of the Window.
        /// </summary>
        public Channel<RectangleF>? Bounds { private get; set; }
        ChannelFlange<RectangleF> BoundsFlange = new ChannelFlange<RectangleF>(new RectangleF(0f, 0f, 1f, 1f));

        /// <summary>
        /// Returns true if the Window is visible. Returns false if the close button is clicked.
        /// </summary>
        /// It is not a channel, because it can't be used to set visibility of the window.
        /// https://github.com/ocornut/imgui/blob/5bb287494096461f90eb5d18135f7c4809efd2f5/imgui.h#L320
        /// 
        public bool isVisible { get; private set; } = false;

        /// <summary>
        /// Returns true if the Window is open (not collapsed or clipped). Set to true to open the window.
        /// </summary>
        public Channel<bool>? IsOpen { private get; set; }
        ChannelFlange<bool> IsOpenFlange = new ChannelFlange<bool>(true);
        /// <summary>
        /// Returns true if the Window is open (not collapsed or clipped). 
        /// </summary>
        public bool _IsOpen => IsOpenFlange.Value;

        public ImGuiWindowFlags Flags { get; set; }

        internal override void UpdateCore(Context context)
        {
            var isOpen = IsOpenFlange.Update(IsOpen);

            var bounds = BoundsFlange.Update(Bounds, out bool boundsChanged);

            if (boundsChanged)
            {
                ImGui.SetNextWindowPos (bounds.TopLeft.FromHectoToImGui());
                ImGui.SetNextWindowSize (bounds.Size.FromHectoToImGui());
            }

            ImGui.SetNextWindowCollapsed(!isOpen);

            if (HasCloseButton)
            {
                var visible = true;
                isOpen = ImGui.Begin(Name, ref visible, Flags);
                isVisible = visible; // close button might have been pressed
            }
            else
            {
                isOpen = ImGui.Begin(Name, Flags);
            }

            try
            {
                IsOpenFlange.Value = isOpen;

                if (isOpen)
                {
                    context.Update(Content);
                }     
            }
            finally
            {
                ImGui.End();
            }
        }
    }
}
