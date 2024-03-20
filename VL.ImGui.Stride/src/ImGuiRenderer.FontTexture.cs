using ImGuiNET;
using Stride.Graphics;

namespace VL.ImGui
{
    partial class ImGuiRenderer
    {
        void BuildImFontAtlas(GraphicsDevice device, ImFontAtlasPtr atlas, float scaling)
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

                fontTexture?.Dispose();

                var newFontTexture = Texture.New2D(
                    device: device,
                    width: width,
                    height: height,
                    mipCount: 1,
                    format: device.ColorSpace == ColorSpace.Linear ? PixelFormat.R8G8B8A8_UNorm_SRgb : PixelFormat.R8G8B8A8_UNorm,
                    textureData: [new DataBox(new IntPtr(pixelData), rowPitch: width * bytesPerPixel, slicePitch: width * height * bytesPerPixel)],
                    textureFlags: TextureFlags.ShaderResource,
                    usage: GraphicsResourceUsage.Immutable);

                fontTexture = newFontTexture;
                atlas.ClearTexData();
            }
        }
    }
}
