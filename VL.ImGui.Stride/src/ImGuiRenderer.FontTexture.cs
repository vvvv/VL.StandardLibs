using ImGuiNET;
using SixLabors.Fonts;
using Stride.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.Collections;

namespace VL.ImGui
{
    partial class ImGuiRenderer
    {
        void BuildImFontAtlas(ImFontAtlasPtr atlas, float scaling)
        {
            atlas.Clear();
            _context.Fonts.Clear();

            var anyFontLoaded = false;
            var fontsfolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

            if (fonts == null || (fonts.IsEmpty && FontConfig.Default != null))
                fonts = Spread.Create(FontConfig.Default);

            foreach (var font in fonts)
            {
                if (font is null)
                    continue;

                var size = Math.Clamp(font.Size * 100 /* hecto pixel */ * scaling, 1, short.MaxValue);

                var family = SystemFonts.Families.FirstOrDefault(f => f.Name == font.FamilyName.Value);
                if (family.Name is null)
                    continue;

                var systemFont = family.CreateFont((float)(size * 0.75f) /* PT */, font.FontStyle);
                if (!systemFont.TryGetPath(out var path))
                    continue;

                ImFontConfig cfg = new ImFontConfig()
                {
                    SizePixels = size,
                    FontDataOwnedByAtlas = 0,
                    EllipsisChar = unchecked((ushort)-1),
                    OversampleH = 2,
                    OversampleV = 1,
                    PixelSnapH = 1,
                    GlyphOffset = new System.Numerics.Vector2(0, 0),
                    GlyphMaxAdvanceX = float.MaxValue,
                    RasterizerMultiply = 1.0f,
                    RasterizerDensity = 1.0f
                };

                unsafe
                {
                    // Write name
                    Span<byte> s = Encoding.Default.GetBytes(font.ToString());
                    var dst = new Span<byte>(cfg.Name, 40);
                    s.Slice(0, Math.Min(s.Length, dst.Length)).CopyTo(dst);

                    // TODO this caused a Memory leak ... old Font will not disposed?? 
                    var f = atlas.AddFontFromFileTTF(path, cfg.SizePixels, &cfg, GetGlypthRange(atlas, font.GlyphRange));
                    anyFontLoaded = true;
                    _context.Fonts[font.Name] = f;
                }
            }

            if (!anyFontLoaded)
            {
                atlas.AddFontDefault();
            }


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

                var newFontTexture = Texture.New2D(device, width, height, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource);
                newFontTexture.SetData(commandList, new DataPointer(pixelData, (width * height) * bytesPerPixel));
                fontTexture?.Dispose();
                fontTexture = newFontTexture;
                atlas.ClearTexData();
            }

            static IntPtr GetGlypthRange(ImFontAtlasPtr atlas, GlyphRange glyphRange)
            {
                return glyphRange switch
                {
                    GlyphRange.ChineseFull => atlas.GetGlyphRangesChineseFull(),
                    GlyphRange.ChineseSimplifiedCommon => atlas.GetGlyphRangesChineseSimplifiedCommon(),
                    GlyphRange.Cyrillic => atlas.GetGlyphRangesCyrillic(),
                    GlyphRange.Greek => atlas.GetGlyphRangesGreek(),
                    GlyphRange.Japanese => atlas.GetGlyphRangesJapanese(),
                    GlyphRange.Korean => atlas.GetGlyphRangesKorean(),
                    GlyphRange.Thai => atlas.GetGlyphRangesThai(),
                    GlyphRange.Vietnamese => atlas.GetGlyphRangesVietnamese(),
                    _ => atlas.GetGlyphRangesDefault()
                };
            }
        }
    }
}
