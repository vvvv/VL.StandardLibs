using ImGuiNET;
using SkiaSharp;
using Stride.Core.Mathematics;
using VL.ImGui.Widgets.Primitives;
using VL.Lib.IO.Notifications;
using VL.Skia;

namespace VL.ImGui.Widgets
{
    using ImGui = ImGuiNET.ImGui;

    [GenerateNode(Category = "ImGui.Widgets.Internal", IsStylable = false)]
    public sealed partial class SkiaWidget : PrimitiveWidget, IDisposable, ILayer
    {
        private bool _disposed;
        private bool _itemHasFocus;
        private bool _windowHasFocus;
        private IContextWithSkia? _skiaContext;

        public ILayer? Layer { private get; set; }

        public Vector2 Size { private get; set; } = new Vector2(1f, 1f);

        /// <summary>
        /// Controls in which state events are allowed to pass through.
        /// </summary>
        public EventFilter EventFilter { private get; set; } = EventFilter.ItemHasFocus;

        protected override void Draw(Context context, in ImDrawListPtr drawList, in System.Numerics.Vector2 offset)
        {
            if (context is IContextWithSkia skiaContext)
            {
                this._skiaContext = skiaContext;

                if (Layer is null)
                {
                    skiaContext.RemoveLayer(this);
                    return;
                }

                var pos = ImGui.GetCursorPos();
                var size = Size.FromHectoToImGui();
                ImGui.InvisibleButton($"{GetHashCode()}", size, ImGuiButtonFlags.None);
                _itemHasFocus = ImGui.IsItemFocused();
                _windowHasFocus = ImGui.IsWindowFocused();
                ImGui.SetCursorPos(pos);

                // Use Callback instead of Texture to pass Layer ID ... first Layer has ID 1
                var id = skiaContext.AddLayer(this, pos, size);

                // Why is this no longer necessary?
                //if (context.DrawList == DrawList.AtCursor)
                //    ImGui.Image(0, size);

                // because we use Callback instead of Image  


                drawList.AddCallback(id, IntPtr.Zero);
            }
        }

        [Pin(Ignore = true)]
        public RectangleF? Bounds => !_disposed ? Layer?.Bounds : default;

        public void Render(CallerInfo caller)
        {
            if (_disposed || Layer is null)
                return;
            Layer.Render(caller);
        }

        public bool Notify(INotification notification, CallerInfo caller)
        {
            if (_disposed || Layer is null)
                return false;

            if (EventFilter == EventFilter.BlockAll)
                return false;
            if (EventFilter == EventFilter.ItemHasFocus && !_itemHasFocus)
                return false;
            if (EventFilter == EventFilter.WindowHasFocus && !_windowHasFocus)
                return false;

            return Layer.Notify(notification, caller); ;
        }

        public void Dispose()
        {
            _skiaContext?.RemoveLayer(this);
            _disposed = true;
        }
    }

    public enum EventFilter
    {
        /// <summary>
        /// Blocks all events.
        /// </summary>
        BlockAll,
        /// <summary>
        /// Events will only pass through if the drawing area has focus.
        /// </summary>
        ItemHasFocus,
        /// <summary>
        /// Events will only pass through if the surrounding ImGui window has focus.
        /// </summary>
        WindowHasFocus,
        /// <summary>
        /// Events will always pass through.
        /// </summary>
        AllowAll
    }
}
