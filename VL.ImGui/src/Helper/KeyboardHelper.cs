using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.IO;

namespace VL.ImGui
{
    internal static class KeyboardHelper
    {
        private static readonly Dictionary<ImGuiKey, Keys> s_reverseMap = new Dictionary<ImGuiKey, Keys>();

        static KeyboardHelper()
        {
            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                var imguiKey = key.ToImGuiKey();
                if (imguiKey != ImGuiKey.None)
                    s_reverseMap[imguiKey] = key;
            }
        }

        public static Keys ToKeys(this ImGuiKey key)
        {
            if (s_reverseMap.TryGetValue(key, out var keys))
                return keys;
            return Keys.None;
        }

        public static ImGuiKey ToImGuiKey(this VL.Lib.IO.Keys key)
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
}
