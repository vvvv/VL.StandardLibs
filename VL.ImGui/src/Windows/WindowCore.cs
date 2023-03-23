using Stride.Core.Mathematics;
using ImGuiNET;
using VL.Lib.Reactive;
using System.Reactive;

namespace VL.ImGui.Windows
{
    using ImGui = ImGuiNET.ImGui;

    [GenerateNode(Category = "ImGui.Widgets.Internal", GenerateRetained = false)]
    internal sealed partial class WindowCore : Widget
    {
        public Widget? Content { get; set; }

        public string Name { get; set; } = "Window";

        /// <summary>
        /// If set the window will have a close button which will push to the channel once clicked.
        /// </summary>
        public Channel<Unit> Closing { get; set; } = DummyChannel<Unit>.Instance;

        /// <summary>
        /// Bounds of the Window.
        /// </summary>
        public Channel<RectangleF>? Bounds { private get; set; }
        ChannelFlange<RectangleF> BoundsFlange = new ChannelFlange<RectangleF>(new RectangleF(0f, 0f, 1f, 1f));

        /// <summary>
        /// Returns true if the Window is open. Set to true to open the window.
        /// </summary>
        /// We bend the original lib here, because in ImGui it can't be used to set visibility of the window.
        /// https://github.com/ocornut/imgui/blob/5bb287494096461f90eb5d18135f7c4809efd2f5/imgui.h#L320
        /// 
        public Channel<bool>? Visible { private get; set; }
        ChannelFlange<bool> VisibleFlange = new ChannelFlange<bool>(true);

        /// <summary>
        /// Returns true if the Window is collapsed. Set to true to collapse the window.
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

        public ImGuiWindowFlags Flags { get; set; }

        internal override void UpdateCore(Context context)
        {
            var visible = VisibleFlange.Update(Visible);
            CloseClicked = false;
            ContentIsVisible = false;

            if (visible)
            {
                var bounds = BoundsFlange.Update(Bounds, out bool boundsChanged);
                var collapsed = CollapsedFlange.Update(Collapsed);

                if (boundsChanged)
                {
                    ImGui.SetNextWindowPos(bounds.TopLeft.FromHectoToImGui());
                    ImGui.SetNextWindowSize(bounds.Size.FromHectoToImGui());
                }

                ImGui.SetNextWindowCollapsed(collapsed);

                if (Closing.IsValid())
                {
                    var isVisible = true;
                    ContentIsVisible = ImGui.Begin(Name, ref isVisible, Flags);
                    if (!isVisible)
                    {
                        Closing.Value = default;
                        CloseClicked = true;
                    }
                        
                }
                else
                {
                    ContentIsVisible = ImGui.Begin(Name, Flags);
                }

                try
                {
                    CollapsedFlange.Value = ImGui.IsWindowCollapsed();

                    if (ContentIsVisible)
                    {
                        context.Update(Content);

                        var pos = ImGui.GetWindowPos().ToVLHecto();
                        var size = ImGui.GetWindowSize().ToVLHecto();
                        BoundsFlange.Value = new RectangleF(pos.X, pos.Y, size.X, size.Y);
                    }
                }
                finally
                {
                    ImGui.End();
                }
            }

        }
    }
}
