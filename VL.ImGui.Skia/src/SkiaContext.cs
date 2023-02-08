using ImGuiNET;
using Stride.Core.Mathematics;
using System.Runtime.CompilerServices;
using VL.Lib.IO.Notifications;
using VL.Skia;

namespace VL.ImGui
{
    using ImGui = ImGuiNET.ImGui;

    class EmptyLayer : ILayer
    {
        public static EmptyLayer Instance = new EmptyLayer();

        public RectangleF? Bounds => default;

        public bool Notify(INotification notification, CallerInfo caller)
        {
            return false;
        }

        public void Render(CallerInfo caller)
        {
        }
    }

    internal sealed class SkiaContext : Context
    {
        public readonly List<ILayer> Layers = new List<ILayer>();

        public override void NewFrame()
        {
            Layers.Clear();
            Layers.Add(EmptyLayer.Instance);
            base.NewFrame();
        }

        internal void AddLayer(Vector2 size, ILayer layer)
        {
            var id = Layers.Count;
            Layers.Add(layer);
            DrawListPtr.AddImage(new IntPtr(id), default, Unsafe.As<Vector2, System.Numerics.Vector2>(ref size));
            if (DrawList == DrawList.AtCursor)
                ImGui.Image(new IntPtr(0), Unsafe.As<Vector2, System.Numerics.Vector2>(ref size));
        }
    }
}
