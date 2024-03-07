using Stride.Core.Mathematics;
using Stride.Graphics;
using System.Runtime.InteropServices;

namespace VL.ImGui.Widgets
{
    using ImGui = ImGuiNET.ImGui;

    [GenerateNode(Category = "ImGui.Widgets.Internal", IsStylable = false)]
    public sealed partial class TextureWidget : Widget, IDisposable
    {
        private GCHandle textureHandle;

        public Texture? Texture { private get; set; }

        public Vector2 Size { private get; set; } = new Vector2(1f, 1f);

        internal override void UpdateCore(Context context)
        {
            if (Texture is null)
                return;

            if (context is StrideContext strideContext)
            {
                if (textureHandle.IsAllocated)
                    textureHandle.Free();

                textureHandle = GCHandle.Alloc(Texture);
                ImGui.Image(GCHandle.ToIntPtr(textureHandle), Size.FromHectoToImGui());
            }
        }
        public void Dispose()
        {
            if (textureHandle.IsAllocated)
                textureHandle.Free();
        }
    }
}
