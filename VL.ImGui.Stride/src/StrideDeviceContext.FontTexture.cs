using ImGuiNET;
using Stride.Core.Mathematics;
using Stride.Graphics;
using System.Runtime.InteropServices;
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
                _io.Fonts.BuildImFontAtlas(this, _fonts);
            }
        }

        unsafe void UpdateTexture(CommandList commandList, ImTextureDataPtr textureData)
        {
            if (textureData.Status == ImTextureStatus.WantCreate)
            {
                var width = textureData.Width;
                var height = textureData.Height;
                var rowPitch = textureData.GetPitch();
                var pixelData = textureData.Pixels;
                var texture = Texture.New2D(
                    device: device,
                    width: width,
                    height: height,
                    mipCount: 1,
                    format: device.ColorSpace == ColorSpace.Linear ? PixelFormat.R8G8B8A8_UNorm_SRgb : PixelFormat.R8G8B8A8_UNorm,
                    textureData: [new DataBox(pixelData, rowPitch: rowPitch, slicePitch: rowPitch * height)],
                    textureFlags: TextureFlags.ShaderResource,
                    usage: GraphicsResourceUsage.Default);
                textureData.TexID = GCHandle.ToIntPtr(GCHandle.Alloc(texture));
                textureData.SetStatus(ImTextureStatus.OK);
            }
            else if (textureData.Status == ImTextureStatus.WantUpdates)
            {
                var texture = GCHandle.FromIntPtr(textureData.TexID).Target as Texture;
                if (texture is null)
                    return;

                for (int i = 0; i < textureData.Updates.Size; i++)
                {
                    var rect = textureData.Updates[i];
                    var rowBytes = (uint)(rect.w * sizeof(Color));
                    var data = new Span<Color>(textureData.GetPixelsAt(rect.x, rect.y).ToPointer(), textureData.GetPitch());
                    texture.SetData(commandList, data, region: new ResourceRegion(rect.x, rect.y, 0, rect.x + rect.w, rect.y + rect.h, 1));
                }

                textureData.SetStatus(ImTextureStatus.OK);
            }
            else if (textureData.Status == ImTextureStatus.WantDestroy && textureData.UnusedFrames > 0)
            {
                var handle = GCHandle.FromIntPtr(textureData.TexID);
                var texture = handle.Target as Texture;
                texture?.Dispose();
                handle.Free();

                textureData.SetTexID(default);
                textureData.SetStatus(ImTextureStatus.Destroyed);
            }
        }
    }
}
