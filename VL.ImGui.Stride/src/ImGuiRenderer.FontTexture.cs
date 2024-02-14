using ImGuiNET;
using Stride.Graphics;

namespace VL.ImGui
{
    partial class ImGuiRenderer
    {
        void BuildImFontAtlas(ImFontAtlasPtr atlas, float scaling)
        {
            atlas.BuildImFontAtlas(scaling, _context, fonts);

            unsafe
            {
                atlas.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);
                if (width == 0 || height == 0)
                {
                    // Something went wrong, load default font
                    atlas.Clear();
                    _context.Fonts.Clear();
                    atlas.AddFontDefault();
                    atlas.GetTexDataAsRGBA32(out pixelData, out width, out height, out bytesPerPixel);
                }

                var newFontTexture = Texture.New2D(device, width, height, device.ColorSpace == ColorSpace.Linear ? PixelFormat.R8G8B8A8_UNorm_SRgb : PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource);
                newFontTexture.SetData(commandList, new DataPointer(pixelData, (width * height) * bytesPerPixel));
                fontTexture?.Dispose();
                fontTexture = newFontTexture;
                atlas.ClearTexData();
            }
        }
    }
}
