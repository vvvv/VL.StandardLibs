using ImGuiNET;
using SkiaSharp;
using Stride.Core.Mathematics;
using VL.ImGui.Widgets.Primitives;
using VL.Lib.IO.Notifications;
using VL.Skia;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets.Internal", IsStylable = false)]
    public sealed partial class SkiaWidget : PrimitiveWidget, IDisposable, ILayer
    {
        private bool _disposed;
        private bool _hasFocus;

        public ILayer? Layer { private get; set; }

        public Vector2 Size { private get; set; } = new Vector2(1f, 1f);

        protected override void Draw(Context context, in ImDrawListPtr drawList, in System.Numerics.Vector2 offset)
        {
            if (context is SkiaContext skiaContext)
            {
                var _ = Size.FromHectoToImGui();
                if (Layer != null)
                {
                    var position = ImGuiNET.ImGui.GetCursorPos();
                    ImGuiNET.ImGui.InvisibleButton($"{GetHashCode()}", _, ImGuiButtonFlags.None);
                    _hasFocus = ImGuiNET.ImGui.IsItemFocused();
                    ImGuiNET.ImGui.SetCursorPos(position);
                    skiaContext.AddLayer(new Vector2(_.X, _.Y), this);
                }
            }
        }

        [Pin(Ignore = true)]
        public RectangleF? Bounds => !_disposed ? Layer?.Bounds : default;

        public void Render(CallerInfo caller)
        {
            if (!_disposed)
                Layer?.Render(caller);
        }

        public bool Notify(INotification notification, CallerInfo caller)
        {
            if (!_disposed && Layer != null && _hasFocus)
                return Layer.Notify(notification, caller);
            return false;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
