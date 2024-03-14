using ImGuiNET;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Rendering;
using System.Reactive.Disposables;
using Viewport = Stride.Graphics.Viewport;

namespace VL.ImGui.Widgets
{
    using ImGui = ImGuiNET.ImGui;

    [GenerateNode(Category = "ImGui.Widgets.Internal", IsStylable = false)]
    public sealed partial class RenderWidget : Widget, IDisposable
    {
        private readonly RenderLayer renderLayer;
        private IContextWithRenderer? strideContext;
        private readonly SerialDisposable inputSubscription = new SerialDisposable();
        private bool _itemHasFocus;
        private bool _windowHasFocus;


        public RenderWidget()
        {
            renderLayer = new RenderLayer();
        }

        public IGraphicsRendererBase? Layer { private get; set ; }

        public RenderView? RenderView { private get; set; } = new RenderView();

        public Vector2 Size { private get; set; } = new Vector2(1f, 1f);

        /// <summary>
        /// Controls in which state events are allowed to pass through.
        /// </summary>
        public EventFilter EventFilter { private get; set; } = EventFilter.ItemHasFocus;

        // TODO Mapp InputSource
        public IInputSource? InputSource { 
            get   
            {
                if (EventFilter == EventFilter.BlockAll)
                    return null;
                if (EventFilter == EventFilter.ItemHasFocus && !_itemHasFocus)
                    return null;
                if (EventFilter == EventFilter.WindowHasFocus && !_windowHasFocus)
                    return null;

                // TODO USE InputSourceSimulated 
                return renderLayer.InputSource; 
            }  
        }

        internal override void UpdateCore(Context context)
        {
            if (context is StrideContext strideContext)
            {
                this.strideContext = strideContext;

                if (Layer is null)
                {
                    strideContext.RemoveRenderer(renderLayer);
                    return;
                }
                

                if (renderLayer.RenderView != RenderView)
                    renderLayer.RenderView = RenderView;

                if  (renderLayer.Layer != Layer)
                    renderLayer.Layer = Layer;

                var id = strideContext.AddRenderer(renderLayer);
  

                if (ImGui.BeginChild("##RenderWidget__" + id.ToString(), Size.FromHectoToImGui(), ImGuiChildFlags.None, ImGuiWindowFlags.ChildWindow))
                {
                    var pos = ImGui.GetWindowPos();
                    var size = ImGui.GetWindowSize();

                    ImGui.InvisibleButton($"{GetHashCode()}", size, ImGuiButtonFlags.None);
                    _itemHasFocus = ImGui.IsItemFocused();
                    _windowHasFocus = ImGui.IsWindowFocused();

                    renderLayer.Viewport = new Viewport(pos.X, pos.Y, size.X, size.Y);
                    var drawList = ImGui.GetWindowDrawList();
                    drawList.AddCallback(id, IntPtr.Zero);
                }
                ImGui.EndChild();
            }
        }

        public void Dispose()
        {
            strideContext?.RemoveRenderer(renderLayer);
        }
    }
}
