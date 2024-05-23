using ImGuiNET;
using SixLabors.Fonts;
using System.Numerics;
using System.Text;
using VL.Core;
using VL.Lib.Collections;
using VL.Lib.IO.Notifications;

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

        public static void HandleNotification(this ImGuiIOPtr _io, INotification notification)
        {
            if (notification is KeyNotification keyNotification)
            {
                // Submit modifiers, see https://github.com/ocornut/imgui/blob/master/backends/imgui_impl_win32.cpp#L620
                _io.AddKeyEvent(ImGuiKey.ModCtrl, keyNotification.CtrlKey);
                _io.AddKeyEvent(ImGuiKey.ModShift, keyNotification.ShiftKey);
                _io.AddKeyEvent(ImGuiKey.ModAlt, keyNotification.AltKey);

                if (keyNotification is KeyCodeNotification keyCodeNotification)
                {
                    _io.AddKeyEvent(ToImGuiKey(keyCodeNotification.KeyCode), keyCodeNotification.IsKeyDown);
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

                // The up & down event methods don't take the position as an argument. Therefor make sure it's present, or we end up with wrong clicks when using touch devices.
                var pos = mouseNotification.PositionInWorldSpace.FromHectoToImGui();
                _io.AddMousePosEvent(pos.X, pos.Y);

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
                            _io.AddMouseWheelEvent(0, wheel.WheelDelta / 120);
                        break;
                    case MouseNotificationKind.MouseHorizontalWheel:
                        if (mouseNotification is MouseHorizontalWheelNotification hWheel)
                            _io.AddMouseWheelEvent(hWheel.WheelDelta / 120, 0);
                        break;
                    case MouseNotificationKind.DeviceLost:
                        _io.ClearInputKeys();
                        _io.AddFocusEvent(false);
                        break;
                    default:
                        break;
                }
            }
            else if (notification is TouchNotification touchNotification)
            {
                // ImGui has no touch - we rely on the mouse emulation of the event system
            }
            else if (notification is LostFocusNotification)
            {
                _io.ClearInputKeys();
                _io.AddFocusEvent(false);
            }


            static ImGuiKey ToImGuiKey(VL.Lib.IO.Keys key)
            {
                switch (key)
                {
                    case Keys.Back: return ImGuiKey.Backspace;
                    case Keys.Tab: return ImGuiKey.Tab;
                    case Keys.Enter: return ImGuiKey.Enter;
                    case Keys.ShiftKey: return ImGuiKey.LeftShift;
                    case Keys.ControlKey: return ImGuiKey.LeftCtrl;
                    case Keys.Menu: return ImGuiKey.Menu;
                    case Keys.Pause: return ImGuiKey.Pause;
                    case Keys.CapsLock: return ImGuiKey.CapsLock;
                    case Keys.Escape: return ImGuiKey.Escape;
                    case Keys.Space: return ImGuiKey.Space;
                    case Keys.PageUp: return ImGuiKey.PageUp;
                    case Keys.PageDown: return ImGuiKey.PageDown;
                    case Keys.End: return ImGuiKey.End;
                    case Keys.Home: return ImGuiKey.Home;
                    case Keys.Left: return ImGuiKey.LeftArrow;
                    case Keys.Up: return ImGuiKey.UpArrow;
                    case Keys.Right: return ImGuiKey.RightArrow;
                    case Keys.Down: return ImGuiKey.DownArrow;
                    case Keys.Snapshot: return ImGuiKey.PrintScreen;
                    case Keys.Insert: return ImGuiKey.Insert;
                    case Keys.Delete: return ImGuiKey.Delete;
                    case Keys.Oem7: return ImGuiKey.Apostrophe;
                    case Keys.Oemcomma: return ImGuiKey.Comma;
                    case Keys.OemMinus: return ImGuiKey.Minus;
                    case Keys.OemPeriod: return ImGuiKey.Period;
                    case Keys.Oem2: return ImGuiKey.Slash;
                    case Keys.Oem1: return ImGuiKey.Semicolon;
                    case Keys.Oemplus: return ImGuiKey.Equal;
                    case Keys.Oem4: return ImGuiKey.LeftBracket;
                    case Keys.Oem5: return ImGuiKey.Backslash;
                    case Keys.Oem6: return ImGuiKey.RightBracket;
                    case Keys.Oem3: return ImGuiKey.GraveAccent;
                    case Keys.D0: return ImGuiKey._0;
                    case Keys.D1: return ImGuiKey._1;
                    case Keys.D2: return ImGuiKey._2;
                    case Keys.D3: return ImGuiKey._3;
                    case Keys.D4: return ImGuiKey._4;
                    case Keys.D5: return ImGuiKey._5;
                    case Keys.D6: return ImGuiKey._6;
                    case Keys.D7: return ImGuiKey._7;
                    case Keys.D8: return ImGuiKey._8;
                    case Keys.D9: return ImGuiKey._9;
                    case Keys.A: return ImGuiKey.A;
                    case Keys.B: return ImGuiKey.B;
                    case Keys.C: return ImGuiKey.C;
                    case Keys.D: return ImGuiKey.D;
                    case Keys.E: return ImGuiKey.E;
                    case Keys.F: return ImGuiKey.F;
                    case Keys.G: return ImGuiKey.G;
                    case Keys.H: return ImGuiKey.H;
                    case Keys.I: return ImGuiKey.I;
                    case Keys.J: return ImGuiKey.J;
                    case Keys.K: return ImGuiKey.K;
                    case Keys.L: return ImGuiKey.L;
                    case Keys.M: return ImGuiKey.M;
                    case Keys.N: return ImGuiKey.N;
                    case Keys.O: return ImGuiKey.O;
                    case Keys.P: return ImGuiKey.P;
                    case Keys.Q: return ImGuiKey.Q;
                    case Keys.R: return ImGuiKey.R;
                    case Keys.S: return ImGuiKey.S;
                    case Keys.T: return ImGuiKey.T;
                    case Keys.U: return ImGuiKey.U;
                    case Keys.V: return ImGuiKey.V;
                    case Keys.W: return ImGuiKey.W;
                    case Keys.X: return ImGuiKey.X;
                    case Keys.Y: return ImGuiKey.Y;
                    case Keys.Z: return ImGuiKey.Z;
                    case Keys.NumPad0: return ImGuiKey.Keypad0;
                    case Keys.NumPad1: return ImGuiKey.Keypad1;
                    case Keys.NumPad2: return ImGuiKey.Keypad2;
                    case Keys.NumPad3: return ImGuiKey.Keypad3;
                    case Keys.NumPad4: return ImGuiKey.Keypad4;
                    case Keys.NumPad5: return ImGuiKey.Keypad5;
                    case Keys.NumPad6: return ImGuiKey.Keypad6;
                    case Keys.NumPad7: return ImGuiKey.Keypad7;
                    case Keys.NumPad8: return ImGuiKey.Keypad8;
                    case Keys.NumPad9: return ImGuiKey.Keypad9;
                    case Keys.Multiply: return ImGuiKey.KeypadMultiply;
                    case Keys.Add: return ImGuiKey.KeypadAdd;
                    case Keys.Subtract: return ImGuiKey.KeypadSubtract;
                    case Keys.Decimal: return ImGuiKey.KeypadDecimal;
                    case Keys.Divide: return ImGuiKey.KeypadDivide;
                    case Keys.F1: return ImGuiKey.F1;
                    case Keys.F2: return ImGuiKey.F2;
                    case Keys.F3: return ImGuiKey.F3;
                    case Keys.F4: return ImGuiKey.F4;
                    case Keys.F5: return ImGuiKey.F5;
                    case Keys.F6: return ImGuiKey.F6;
                    case Keys.F7: return ImGuiKey.F7;
                    case Keys.F8: return ImGuiKey.F8;
                    case Keys.F9: return ImGuiKey.F9;
                    case Keys.F10: return ImGuiKey.F10;
                    case Keys.F11: return ImGuiKey.F11;
                    case Keys.F12: return ImGuiKey.F12;
                    case Keys.F13: return ImGuiKey.F13;
                    case Keys.F14: return ImGuiKey.F14;
                    case Keys.F15: return ImGuiKey.F15;
                    case Keys.F16: return ImGuiKey.F16;
                    case Keys.F17: return ImGuiKey.F17;
                    case Keys.F18: return ImGuiKey.F18;
                    case Keys.F19: return ImGuiKey.F19;
                    case Keys.F20: return ImGuiKey.F20;
                    case Keys.F21: return ImGuiKey.F21;
                    case Keys.F22: return ImGuiKey.F22;
                    case Keys.F23: return ImGuiKey.F23;
                    case Keys.F24: return ImGuiKey.F24;
                    case Keys.BrowserBack: return ImGuiKey.AppBack;
                    case Keys.BrowserForward: return ImGuiKey.AppForward;
                    case Keys.NumLock: return ImGuiKey.NumLock;
                    case Keys.Scroll: return ImGuiKey.ScrollLock;
                    case Keys.LShiftKey: return ImGuiKey.LeftShift;
                    case Keys.RShiftKey: return ImGuiKey.RightShift;
                    case Keys.LControlKey: return ImGuiKey.LeftCtrl;
                    case Keys.RControlKey: return ImGuiKey.RightCtrl;
                    case Keys.LMenu: return ImGuiKey.LeftAlt;
                    case Keys.RMenu: return ImGuiKey.RightAlt;
                    case Keys.LWin: return ImGuiKey.LeftSuper;
                    case Keys.RWin: return ImGuiKey.RightSuper;
                    default:
                        return ImGuiKey.None;
                }
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

            return atlas;

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
