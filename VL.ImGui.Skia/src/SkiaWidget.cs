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

        public ILayer? Layer { private get; set; }

        public Vector2 Size { private get; set; } = new Vector2(1f, 1f);

        /// <summary>
        /// Controls in which state events are allowed to pass through.
        /// </summary>
        public EventFilter EventFilter { private get; set; } = EventFilter.ItemHasFocus;

        protected override void Draw(Context context, in ImDrawListPtr drawList, in System.Numerics.Vector2 offset)
        {
            if (Layer is null)
                return;

            if (context is SkiaContext skiaContext)
            {
                var _ = Size.FromHectoToImGui();
                var position = ImGui.GetCursorPos();
                ImGui.InvisibleButton($"{GetHashCode()}", _, ImGuiButtonFlags.None);
                _itemHasFocus = ImGui.IsItemFocused();
                _windowHasFocus = ImGui.IsWindowFocused();
                ImGui.SetCursorPos(position);
                skiaContext.AddLayer(new Vector2(_.X, _.Y), this);
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

            return Layer.Notify(notification, caller);
        }

        public void Dispose()
        {
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
