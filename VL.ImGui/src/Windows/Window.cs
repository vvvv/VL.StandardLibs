using ImGuiNET;
using Stride.Core.Mathematics;
using System.ComponentModel;
using System.Reactive;
using VL.Core;
using VL.Core.Import;
using VL.Core.PublicAPI;
using VL.ImGui.Internal;
using VL.ImGui.Widgets;
using VL.Lib.Primitive;
using VL.Lib.Reactive;

[assembly: ImportType(typeof(Window))]

namespace VL.ImGui.Widgets
{
    using ImGui = ImGuiNET.ImGui;


    [ProcessNode(FragmentSelection = FragmentSelection.Explicit)]
    [Region(SupportedBorderControlPoints = ControlPointType.Border)]
    public sealed class Window : RegionWidget, IRegion<RegionWidget.IInlay> /* TODO: Fix compiler, redeclaration shouldn't be needed */
    {
        // UniqueId is used for Uniqe Name, so Menu Layout can be saved
        private readonly UniqueId uniqueId;

        private string? name;
        private string? imguiLabel;
        private Optional<IChannel<Unit>> closing;
        private IChannel<RectangleF>? bounds;
        private ChannelFlange<RectangleF> boundsFlange = new ChannelFlange<RectangleF>(new RectangleF(0f, 0f, 1f, 1f));
        private IChannel<bool>? visible;
        private ChannelFlange<bool> visibleFlange = new ChannelFlange<bool>(true);
        private IChannel<bool>? collapsed;
        private ChannelFlange<bool> collapsedFlange = new ChannelFlange<bool>(false);
        private bool contentIsVisible = false;
        private bool closeClicked = false;
        private ImGuiWindowFlags flags;
        private ProcessLifespan lifespan;

        [Fragment]
        public Window(NodeContext nodeContext)
        {
            // ?? why Pop().Peek(), Peek() always the same
            uniqueId = nodeContext.Path.Stack.Pop().Peek();
        }

        /// <param name="context">The current context.</param>
        /// <param name="name">The name of the window.</param>
        /// <param name="visible">Set to true to open the window.</param>
        /// <param name="collapsed">Set to true to collapse the window.</param>
        /// <param name="closing">If set the window will have a close button which will push to the channel once clicked.</param>
        /// <param name="bounds">Bounds of the Window.</param>
        /// <param name="style">The style to apply to the window.</param>
        /// <param name="flags">Window flags to control behavior and appearance.</param>
        /// <param name="lifespan">The process lifespan for resource management.</param>
        /// <param name="closeClicked">Returns true if close button is clicked.</param>
        /// <param name="contentIsVisible">Returns true if content is visible.</param>
        [Fragment]
        [return: VL.Core.Import.Pin(Name = "Context")]
        public Context? Update(
            Context? context, 
            string name, 
            [DefaultValue(true)] IChannel<bool> visible, 
            IChannel<bool> collapsed, 
            Optional<IChannel<Unit>> closing, 
            IChannel<RectangleF> bounds, 
            IStyle style, 
            ImGuiWindowFlags flags, 
            [DefaultValue(ProcessLifespan.Long)] ProcessLifespan lifespan,
            out bool closeClicked,
            out bool contentIsVisible)
        {

            if (this.name != name)
            {
                this.name = name;
                imguiLabel = name + "##" + uniqueId;
            }
            this.visible = visible;
            this.collapsed = collapsed;
            this.closing = closing;
            this.bounds = bounds;
            Style = style;
            this.flags = flags;
            this.lifespan = lifespan;

            Update(context);

            closeClicked = this.closeClicked;
            contentIsVisible = this.contentIsVisible;

            return context;
        }

        internal override void UpdateCore(Context context)
        {
            var visible = visibleFlange.Update(this.visible);
            closeClicked = false;
            contentIsVisible = false;

            if (visible)
            {
                var bounds = boundsFlange.Update(this.bounds, out bool boundsChanged);
                var collapsed = collapsedFlange.Update(this.collapsed);

                if (boundsChanged)
                {
                    ImGui.SetNextWindowPos(bounds.TopLeft.FromHectoToImGui());
                    ImGui.SetNextWindowSize(bounds.Size.FromHectoToImGui());
                }

                ImGui.SetNextWindowCollapsed(collapsed);

                if (closing.HasValue)
                {
                    //  We bend the original lib here, because in ImGui it can't be used to set visibility of the window. https://github.com/ocornut/imgui/blob/5bb287494096461f90eb5d18135f7c4809efd2f5/imgui.h#L320
                    var isVisible = true;
                    contentIsVisible = ImGui.Begin(widgetLabel.Update(imguiLabel), ref isVisible, flags);
                    if (!isVisible)
                    {
                        closing.Value.Value = default;
                        closeClicked = true;
                    }

                }
                else
                {
                    contentIsVisible = ImGui.Begin(widgetLabel.Update(imguiLabel), flags);
                }

                try
                {
                    collapsedFlange.Value = ImGui.IsWindowCollapsed();

                    if (contentIsVisible)
                    {
                        RunPatchInlay(context);

                        var pos = ImGui.GetWindowPos().ToVLHecto();
                        var size = ImGui.GetWindowSize().ToVLHecto();
                        boundsFlange.Value = new RectangleF(pos.X, pos.Y, size.X, size.Y);
                    }
                }
                finally
                {
                    ImGui.End();
                }
            }
            else
            {
                if (lifespan == ProcessLifespan.Short)
                {
                    // If the window is not visible and we are in a short lifespan, dispose the patch inlay to free resources
                    DisposePatchInlay();
                }
            }
        }
    }
}
