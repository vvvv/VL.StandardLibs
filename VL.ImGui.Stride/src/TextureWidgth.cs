using Stride.Core.Mathematics;
using Stride.Graphics;

namespace VL.ImGui.Widgets
{
    using ImGui = ImGuiNET.ImGui;

    [GenerateNode(Category = "ImGui.Widgets.Internal", IsStylable = false)]
    public sealed partial class TextureWidget : Widget
    {
        private Texture tex = new Texture();

        public Texture? Texture { private get; set; }

        public Vector2 Size { private get; set; } = new Vector2(1f, 1f);

        internal override void UpdateCore(Context context)
        {
            if (Texture is null)
                return;

            if (context is StrideContext strideContext)
            {
                tex = Texture;
                unsafe
                {
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
                    fixed (Texture* ptr = &tex)
                    {
                        ImGui.Image((IntPtr)ptr, Size.FromHectoToImGui());
                    }
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
                }
            }
        }
    }
}
