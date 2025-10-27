﻿using ImGuiNET;
using System.Drawing;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Text;
using VL.Core;
using VL.Lib.Collections;
using VL.Lib.IO.Notifications;
using VL.Lib.Text;

namespace VL.ImGui
{
    using ImGui = ImGuiNET.ImGui;
    using Keys = VL.Lib.IO.Keys;
    using MouseButtons = VL.Lib.IO.MouseButtons;

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

                // If viewports are enabled, ImGui expects the mouse in DesktopScreenSpace, this is set in ImGuiWindows/SetPerFrameImGuiData, if not use the old approach
                var flag = ImGui.GetIO().ConfigFlags;
                if ((flag & ImGuiConfigFlags.ViewportsEnable) == 0)
                {
                    var pos = mouseNotification.PositionInWorldSpace.FromHectoToImGui();
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

        public static ImFontAtlasPtr BuildImFontAtlas(this ImFontAtlasPtr atlas, Context _context, Spread<FontConfig?>? fonts)
        {
            atlas.Clear();
            _context.Fonts.Clear();

            if (fonts is null || fonts.IsEmpty)
                fonts = Spread.Create(FontConfig.Default);

            ImFontPtr defaultFont = default;
            foreach (var font in fonts)
            {
                if (font is null)
                    continue;

                var fontPath = FontListDefinition.GetFontPath(font.FamilyName.Value, font.FontStyle);
                if (fontPath.IsDefault)
                    continue;

                var size = Math.Clamp(font.Size * 100 /* hecto pixel */ /** scaling*/, 1, short.MaxValue);
                ImFontConfig cfg = new ImFontConfig()
                {
                    SizePixels = size,
                    FontNo = (uint)fontPath.Index,
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
                    var f = atlas.AddFontFromFileTTF(fontPath.Path, 0f, &cfg);
                    _context.Fonts[font.Name] = f;
                    if (defaultFont.NativePtr is null)
                        defaultFont = f;
                }
            }

            if (_context.Fonts.Count == 0)
            {
                defaultFont = atlas.AddFontDefault();
            }

            _context.DefaultFont = defaultFont;
            //ImGui.PushFont(defaultFont, 0f);

            return atlas;
        }
    }
}
