using ImGuiNET;
using Stride.Core.Mathematics;
using System.Runtime.CompilerServices;
using VL.Lib.IO.Notifications;
using VL.Skia;

namespace VL.ImGui
{
    public class SkiaContext : Context
    {
        public readonly List<ILayer> Layers = new List<ILayer>();

        public override void NewFrame()
        {
            Layers.Clear();
            base.NewFrame();
        }

        internal void AddLayer(ILayer layer)
        {
            Layers.Add(layer);
        }
    }
}
