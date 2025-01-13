using ImGuiNET;
using SkiaSharp;
using Stride.Graphics;
using VL.Lib.Collections;

namespace VL.ImGui
{
    partial class StrideDeviceContext
    {
        public void SetFonts(Spread<FontConfig?> fonts)
        {
            if (!fonts.IsEmpty && !_fonts.SequenceEqual(fonts))
            {
                _fonts = fonts;
                BuildImFontAtlas(device, _io.Fonts, _fontScaling);
            }
        }

        void BuildImFontAtlas(GraphicsDevice device, ImFontAtlasPtr atlas, float scaling)
        {
            atlas.BuildImFontAtlas(scaling, this, _fonts);

            unsafe
            {
                atlas.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);
                if (width == 0 || height == 0)
                {
                    // Something went wrong, load default font
                    atlas.Clear();
                    this.Fonts.Clear();
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
