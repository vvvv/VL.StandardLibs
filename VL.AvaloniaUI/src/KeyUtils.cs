using Avalonia.Input;
using Avalonia.Input.Raw;
using VL.Lib.IO;

namespace VL.AvaloniaUI
{
    static class KeyUtils
    {
        public static RawInputModifiers ToModifier(this Keys key)
        {
            var modifers = RawInputModifiers.None;
            if (key.HasFlag(Keys.Shift))
                modifers |= RawInputModifiers.Shift;
            if (key.HasFlag(Keys.Control))
                modifers |= RawInputModifiers.Control;
            if (key.HasFlag(Keys.Alt))
                modifers |= RawInputModifiers.Alt;
            return modifers;
        }

        public static RawInputModifiers ToModifier(this MouseButtons button)
        {
            var modifers = RawInputModifiers.None;
            if (button.HasFlag(MouseButtons.Left))
                modifers |= RawInputModifiers.LeftMouseButton;
            if (button.HasFlag(MouseButtons.Middle))
                modifers |= RawInputModifiers.MiddleMouseButton;
            if (button.HasFlag(MouseButtons.Right))
                modifers |= RawInputModifiers.RightMouseButton;
            if (button.HasFlag(MouseButtons.XButton1))
                modifers |= RawInputModifiers.XButton1MouseButton;
            if (button.HasFlag(MouseButtons.XButton2))
                modifers |= RawInputModifiers.XButton2MouseButton;
            return modifers;
        }

        public static RawPointerEventType ToEventType(this MouseButtons button, bool isUp)
        {
            switch (button)
            {
                case MouseButtons.Left:
                    return isUp ? RawPointerEventType.LeftButtonUp : RawPointerEventType.LeftButtonDown;
                case MouseButtons.Right:
                    return isUp ? RawPointerEventType.RightButtonUp : RawPointerEventType.RightButtonDown;
                case MouseButtons.Middle:
                    return isUp ? RawPointerEventType.MiddleButtonUp : RawPointerEventType.MiddleButtonDown;
                case MouseButtons.XButton1:
                    return isUp ? RawPointerEventType.XButton1Up : RawPointerEventType.XButton1Down;
                case MouseButtons.XButton2:
                    return isUp ? RawPointerEventType.XButton2Up : RawPointerEventType.XButton2Down;
                default:
                    return RawPointerEventType.Move;
            }
        }

        public static Key ToKey(this Keys key)
        {
            switch (key & ~Keys.Modifiers)
            {
                case Keys.Cancel:
                    return Key.Cancel;
                case Keys.Back:
                    return Key.Back;
                case Keys.Tab:
                    return Key.Tab;
                case Keys.LineFeed:
                    return Key.LineFeed;
                case Keys.Clear:
                    return Key.Clear;
                case Keys.Return:
                    return Key.Return;
                case Keys.Pause:
                    return Key.Pause;
                case Keys.Capital:
                    return Key.Capital;
                case Keys.KanaMode:
                    return Key.KanaMode;
                case Keys.JunjaMode:
                    return Key.JunjaMode;
                case Keys.FinalMode:
                    return Key.FinalMode;
                case Keys.HanjaMode:
                    return Key.HanjaMode;
                case Keys.Escape:
                    return Key.Escape;
                case Keys.IMEConvert:
                    return Key.ImeConvert;
                case Keys.IMENonconvert:
                    return Key.ImeNonConvert;
                case Keys.IMEAccept:
                    return Key.ImeAccept;
                case Keys.IMEModeChange:
                    return Key.ImeModeChange;
                case Keys.Space:
                    return Key.Space;
                case Keys.Prior:
                    return Key.Prior;
                case Keys.Next:
                    return Key.Next;
                case Keys.End:
                    return Key.End;
                case Keys.Home:
                    return Key.Home;
                case Keys.Left:
                    return Key.Left;
                case Keys.Up:
                    return Key.Up;
                case Keys.Right:
                    return Key.Right;
                case Keys.Down:
                    return Key.Down;
                case Keys.Select:
                    return Key.Select;
                case Keys.Print:
                    return Key.Print;
                case Keys.Execute:
                    return Key.Execute;
                case Keys.Snapshot:
                    return Key.Snapshot;
                case Keys.Insert:
                    return Key.Insert;
                case Keys.Delete:
                    return Key.Delete;
                case Keys.Help:
                    return Key.Help;
                case Keys.D0:
                    return Key.D0;
                case Keys.D1:
                    return Key.D1;
                case Keys.D2:
                    return Key.D2;
                case Keys.D3:
                    return Key.D3;
                case Keys.D4:
                    return Key.D4;
                case Keys.D5:
                    return Key.D5;
                case Keys.D6:
                    return Key.D6;
                case Keys.D7:
                    return Key.D7;
                case Keys.D8:
                    return Key.D8;
                case Keys.D9:
                    return Key.D9;
                case Keys.A:
                    return Key.A;
                case Keys.B:
                    return Key.B;
                case Keys.C:
                    return Key.C;
                case Keys.D:
                    return Key.D;
                case Keys.E:
                    return Key.E;
                case Keys.F:
                    return Key.F;
                case Keys.G:
                    return Key.G;
                case Keys.H:
                    return Key.H;
                case Keys.I:
                    return Key.I;
                case Keys.J:
                    return Key.J;
                case Keys.K:
                    return Key.K;
                case Keys.L:
                    return Key.L;
                case Keys.M:
                    return Key.M;
                case Keys.N:
                    return Key.N;
                case Keys.O:
                    return Key.O;
                case Keys.P:
                    return Key.P;
                case Keys.Q:
                    return Key.Q;
                case Keys.R:
                    return Key.R;
                case Keys.S:
                    return Key.S;
                case Keys.T:
                    return Key.T;
                case Keys.U:
                    return Key.U;
                case Keys.V:
                    return Key.V;
                case Keys.W:
                    return Key.W;
                case Keys.X:
                    return Key.X;
                case Keys.Y:
                    return Key.Y;
                case Keys.Z:
                    return Key.Z;
                case Keys.LWin:
                    return Key.LWin;
                case Keys.RWin:
                    return Key.RWin;
                case Keys.Apps:
                    return Key.Apps;
                case Keys.Sleep:
                    return Key.Sleep;
                case Keys.NumPad0:
                    return Key.NumPad0;
                case Keys.NumPad1:
                    return Key.NumPad1;
                case Keys.NumPad2:
                    return Key.NumPad2;
                case Keys.NumPad3:
                    return Key.NumPad3;
                case Keys.NumPad4:
                    return Key.NumPad4;
                case Keys.NumPad5:
                    return Key.NumPad5;
                case Keys.NumPad6:
                    return Key.NumPad6;
                case Keys.NumPad7:
                    return Key.NumPad7;
                case Keys.NumPad8:
                    return Key.NumPad8;
                case Keys.NumPad9:
                    return Key.NumPad9;
                case Keys.Multiply:
                    return Key.Multiply;
                case Keys.Add:
                    return Key.Add;
                case Keys.Separator:
                    return Key.Separator;
                case Keys.Subtract:
                    return Key.Subtract;
                case Keys.Decimal:
                    return Key.Decimal;
                case Keys.Divide:
                    return Key.Divide;
                case Keys.F1:
                    return Key.F1;
                case Keys.F2:
                    return Key.F2;
                case Keys.F3:
                    return Key.F3;
                case Keys.F4:
                    return Key.F4;
                case Keys.F5:
                    return Key.F5;
                case Keys.F6:
                    return Key.F6;
                case Keys.F7:
                    return Key.F7;
                case Keys.F8:
                    return Key.F8;
                case Keys.F9:
                    return Key.F9;
                case Keys.F10:
                    return Key.F10;
                case Keys.F11:
                    return Key.F11;
                case Keys.F12:
                    return Key.F12;
                case Keys.F13:
                    return Key.F13;
                case Keys.F14:
                    return Key.F14;
                case Keys.F15:
                    return Key.F15;
                case Keys.F16:
                    return Key.F16;
                case Keys.F17:
                    return Key.F17;
                case Keys.F18:
                    return Key.F18;
                case Keys.F19:
                    return Key.F19;
                case Keys.F20:
                    return Key.F20;
                case Keys.F21:
                    return Key.F21;
                case Keys.F22:
                    return Key.F22;
                case Keys.F23:
                    return Key.F23;
                case Keys.F24:
                    return Key.F24;
                case Keys.NumLock:
                    return Key.NumLock;
                case Keys.Scroll:
                    return Key.Scroll;
                case Keys.LShiftKey:
                    return Key.LeftShift;
                case Keys.RShiftKey:
                    return Key.RightShift;
                case Keys.LControlKey:
                    return Key.LeftCtrl;
                case Keys.RControlKey:
                    return Key.RightCtrl;
                case Keys.LMenu:
                    return Key.LeftAlt;
                case Keys.RMenu:
                    return Key.RightAlt;
                case Keys.BrowserBack:
                    return Key.BrowserBack;
                case Keys.BrowserForward:
                    return Key.BrowserForward;
                case Keys.BrowserRefresh:
                    return Key.BrowserRefresh;
                case Keys.BrowserStop:
                    return Key.BrowserStop;
                case Keys.BrowserSearch:
                    return Key.BrowserSearch;
                case Keys.BrowserFavorites:
                    return Key.BrowserFavorites;
                case Keys.BrowserHome:
                    return Key.BrowserHome;
                case Keys.VolumeMute:
                    return Key.VolumeMute;
                case Keys.VolumeDown:
                    return Key.VolumeDown;
                case Keys.VolumeUp:
                    return Key.VolumeUp;
                case Keys.MediaNextTrack:
                    return Key.MediaNextTrack;
                case Keys.MediaPreviousTrack:
                    return Key.MediaPreviousTrack;
                case Keys.MediaStop:
                    return Key.MediaStop;
                case Keys.MediaPlayPause:
                    return Key.MediaPlayPause;
                case Keys.LaunchMail:
                    return Key.LaunchMail;
                case Keys.SelectMedia:
                    return Key.SelectMedia;
                case Keys.LaunchApplication1:
                    return Key.LaunchApplication1;
                case Keys.LaunchApplication2:
                    return Key.LaunchApplication2;
                case Keys.Oem1:
                    return Key.Oem1;
                case Keys.Oemplus:
                    return Key.OemPlus;
                case Keys.Oemcomma:
                    return Key.OemComma;
                case Keys.OemMinus:
                    return Key.OemMinus;
                case Keys.OemPeriod:
                    return Key.OemPeriod;
                case Keys.Oem2:
                    return Key.Oem2;
                case Keys.Oem3:
                    return Key.Oem3;
                case Keys.Oem4:
                    return Key.Oem4;
                case Keys.Oem5:
                    return Key.Oem5;
                case Keys.Oem6:
                    return Key.Oem6;
                case Keys.Oem7:
                    return Key.Oem7;
                case Keys.Oem8:
                    return Key.Oem8;
                case Keys.Oem102:
                    return Key.Oem102;
                case Keys.Attn:
                    return Key.Attn;
                case Keys.Crsel:
                    return Key.CrSel;
                case Keys.Exsel:
                    return Key.ExSel;
                case Keys.EraseEof:
                    return Key.EraseEof;
                case Keys.Play:
                    return Key.Play;
                case Keys.Zoom:
                    return Key.Zoom;
                case Keys.NoName:
                    return Key.NoName;
                case Keys.Pa1:
                    return Key.Pa1;
                case Keys.OemClear:
                    return Key.OemClear;
                default:
                    return Key.None;
            }
        }
    }
}
