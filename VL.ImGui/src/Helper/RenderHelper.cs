using ImGuiNET;
using Stride.Core.Mathematics;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Text;
using VL.Core;
using VL.Lib.Collections;
using VL.Lib.IO.Notifications;
using VL.Lib.Text;

namespace VL.ImGui
{
    using MouseButtons = VL.Lib.IO.MouseButtons;
    using Keys = VL.Lib.IO.Keys;
    using ImGui = ImGuiNET.ImGui;

    static class RenderHelper
    {
        public static unsafe void UpdateUIScaling(float scaling = 1f)
        {
            var style = ImGui.GetStyle();
            style.ScaleAllSizes(scaling);

            // From https://github.com/ocornut/imgui/discussions/3925
            //ImGuiStyle* p = style;
            //ImGuiStyle styleold = *p; // Backup colors
            //var style = new ImGuiStyle(); // IMPORTANT: ScaleAllSizes will change the original size, so we should reset all style config
            //style.WindowBorderSize = 1.0f;
            //style.ChildBorderSize = 1.0f;
            //style.PopupBorderSize = 1.0f;
            //style.FrameBorderSize = 1.0f;
            //style.TabBorderSize = 1.0f;
            //style.WindowRounding = 0.0f;
            //style.ChildRounding = 0.0f;
            //style.PopupRounding = 0.0f;
            //style.FrameRounding = 0.0f;
            //style.ScrollbarRounding = 0.0f;
            //style.GrabRounding = 0.0f;
            //style.TabRounding = 0.0f;
            //style.ScaleAllSizes(scale);
        }

        public static void HandleNotification(this ImGuiIOPtr _io, INotification notification, Func<Vector2, Vector2>? clientToScreen = null)
        {
            if (notification is KeyNotification keyNotification)
            {
                // Submit modifiers, see https://github.com/ocornut/imgui/blob/master/backends/imgui_impl_win32.cpp#L620
                _io.AddKeyEvent(ImGuiKey.ModCtrl, keyNotification.CtrlKey);
                _io.AddKeyEvent(ImGuiKey.ModShift, keyNotification.ShiftKey);
                _io.AddKeyEvent(ImGuiKey.ModAlt, keyNotification.AltKey);

                if (keyNotification is KeyCodeNotification keyCodeNotification)
                {
                    _io.AddKeyEvent(keyCodeNotification.KeyCode.ToImGuiKey(), keyCodeNotification.IsKeyDown);
                }
                else if (keyNotification is KeyPressNotification keyPressNotification)
                {
                    _io.AddInputCharacter(keyPressNotification.KeyChar);
                }
            }
            else if (notification is MouseNotification mouseNotification)
            {
                var button = 0;
                if (mouseNotification is MouseButtonNotification b)
                {
                    button = b.Buttons switch
                    {
                        MouseButtons.Left => 0,
                        MouseButtons.Right => 1,
                        MouseButtons.Middle => 2,
                        MouseButtons.XButton1 => 3,
                        MouseButtons.XButton2 => 4,
                        _ => 0
                    };
                }

                // If viewports are enabled, ImGui expects the mouse in DesktopScreenSpace
                var flag = ImGui.GetIO().ConfigFlags;
                if ((flag & ImGuiConfigFlags.ViewportsEnable) == 0)
                {
                    var pos = mouseNotification.PositionInWorldSpace.FromHectoToImGui();
                    _io.AddMousePosEvent(pos.X, pos.Y);
                }
                else if (clientToScreen != null)
                {
                    var pos = clientToScreen(mouseNotification.Position);
                    _io.AddMousePosEvent(pos.X, pos.Y);
                }

                // The up & down event methods don't take the position as an argument. Therefor make sure it's present, or we end up with wrong clicks when using touch devices.
                switch (mouseNotification.Kind)
                {
                    case MouseNotificationKind.MouseDown:
                        _io.AddMouseButtonEvent(button, true);
                        break;
                    case MouseNotificationKind.MouseUp:
                        _io.AddMouseButtonEvent(button, false);
                        break;
                    case MouseNotificationKind.MouseWheel:
                        if (mouseNotification is MouseWheelNotification wheel)
                            // A notch of a wheel is 120; a trackpad and the wheel of macOS give fractions of it, which an integer division dropped
                            _io.AddMouseWheelEvent(0, wheel.WheelDelta / 120f);
                        break;
                    case MouseNotificationKind.MouseHorizontalWheel:
                        if (mouseNotification is MouseHorizontalWheelNotification hWheel)
                            _io.AddMouseWheelEvent(hWheel.WheelDelta / 120f, 0);
                        break;
                    case MouseNotificationKind.DeviceLost:
                        // The mouse left the window: nothing is hovered any more. The keyboard focus stays (LostFocusNotification ends it)
                        _io.AddMousePosEvent(-float.MaxValue, -float.MaxValue);
                        break;
                    default:
                        break;
                }
            }
            else if (notification is TouchNotification touchNotification)
            {
                // ImGui has no touch - we rely on the mouse emulation of the event system
            }
            else if (notification is GotFocusNotification)
            {
                _io.AddFocusEvent(true);
            }
            else if (notification is LostFocusNotification)
            {
                _io.ClearInputKeys();
                _io.AddFocusEvent(false);
            }
        }

        public static ImFontAtlasPtr BuildImFontAtlas(this ImFontAtlasPtr atlas, float scaling, Context _context, Spread<FontConfig?>? fonts)
        {
            atlas.Clear();
            _context.Fonts.Clear();

            var anyFontLoaded = false;
            var fontsfolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

            if (fonts is null || fonts.IsEmpty)
                fonts = Spread.Create(FontConfig.Default);

            foreach (var font in fonts)
            {
                if (font is null)
                    continue;

                var fontPath = FontListDefinition.GetFontPath(font.FamilyName.Value, font.FontStyle);
                // A custom range asks for code points of that family (the icons of Segoe MDL2 Assets in the private use area): another family has other
                // glyphs there or none, and ImGui crashes building a font without any glyph (ImFont::BuildLookupTable). So only a font of text is replaced
                if (fontPath.IsDefault && font.CustomRanges is null)
                    fontPath = GetSubstitutePath(font.FamilyName.Value, font.FontStyle);
                if (fontPath.IsDefault)
                    continue;

                var size = Math.Clamp(font.Size * 100 /* hecto pixel */ * scaling, 1, short.MaxValue);

                ImFontConfig cfg = new ImFontConfig()
                {
                    SizePixels = size,
                    FontNo = fontPath.Index,
                    FontDataOwnedByAtlas = 1,
                    EllipsisChar = unchecked((ushort)-1),
                    OversampleH = 2,
                    OversampleV = 1,
                    PixelSnapH = 1,
                    GlyphOffset = default,
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

                    var glyphRange = font.CustomGlyphRangePtr;
                    if (glyphRange == default)
                        glyphRange = GetGlypthRange(atlas, font.GlyphRange);
                    var f = atlas.AddFontFromFileTTF(fontPath.Path, cfg.SizePixels, &cfg, glyphRange);
                    anyFontLoaded = true;
                    _context.Fonts[font.Name] = f;
                }
            }

            if (!anyFontLoaded)
            {
                // The font of ImGui (13 pixels) at the scale of the window too
                unsafe
                {
                    var cfg = ImGuiNative.ImFontConfig_ImFontConfig();
                    cfg->SizePixels = 13f * scaling;
                    atlas.AddFontDefault(cfg);
                    ImGuiNative.ImFontConfig_destroy(cfg);
                }
            }

            atlas.Build();

            return atlas;

            // A family which the system does not have (Consolas of the log view on macOS and Linux) is replaced by one of the same kind which it has: before, it was
            // left out, and with nothing loaded ImGui drew its own pixel font of 13 pixels, which does not follow the scale of the window
            static FontListDefinition.FontPath GetSubstitutePath(string family, FontStyle style)
            {
                var monospaced = family.Contains("Mono", StringComparison.OrdinalIgnoreCase) || family.Contains("Consol", StringComparison.OrdinalIgnoreCase)
                    || family.Contains("Courier", StringComparison.OrdinalIgnoreCase) || family.Contains("Code", StringComparison.OrdinalIgnoreCase)
                    || family.Contains("Menlo", StringComparison.OrdinalIgnoreCase);
                string[] substitutes = monospaced
                    ? ["Consolas", "Menlo", "SF Mono", "Monaco", "DejaVu Sans Mono", "Liberation Mono", "Noto Sans Mono", "Ubuntu Mono", "Courier New"]
                    : [FontConfig.Default?.FamilyName.Value ?? "", "Segoe UI", "Helvetica Neue", "Helvetica", "DejaVu Sans", "Liberation Sans", "Arial"];
                foreach (var substitute in substitutes)
                {
                    if (string.IsNullOrEmpty(substitute) || string.Equals(substitute, family, StringComparison.OrdinalIgnoreCase))
                        continue;
                    var path = FontListDefinition.GetFontPath(substitute, style);
                    if (!path.IsDefault)
                        return path;
                }
                return default;
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
