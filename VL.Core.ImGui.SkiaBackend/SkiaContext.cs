using ImGuiNET;
using Stride.Core.Mathematics;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;
using VL.Skia;

namespace VL.ImGui
{
    using ImGui = ImGuiNET.ImGui;

    class EmptyLayer : ILayer
    {
        public static EmptyLayer Instance = new EmptyLayer();

        public RectangleF? Bounds => default;

        public bool Notify(INotification notification, CallerInfo caller)
        {
            return false;
        }

        public void Render(CallerInfo caller)
        {
        }
    }

    public sealed class SkiaContext : Context
    {
        public readonly List<ILayer> Layers = new List<ILayer>();

        public override void NewFrame()
        {
            Layers.Clear();
            Layers.Add(EmptyLayer.Instance);
            base.NewFrame();
        }

        public void AddLayer(Vector2 size, ILayer layer)
        {
            var id = Layers.Count;
            Layers.Add(layer);
            if (DrawList == DrawList.AtCursor)
                ImGui.Image(new IntPtr(id), Unsafe.As<Vector2, System.Numerics.Vector2>(ref size));
            else
                DrawListPtr.AddImage(new IntPtr(id), default, Unsafe.As<Vector2, System.Numerics.Vector2>(ref size));
        }

        public bool Notify(INotification notification, CallerInfo caller, bool applyHectoPixelScaling)
        {
            using (MakeCurrent())
            {
                var _io = ImGui.GetIO();

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
                    var pos = mouseNotification.PositionInWorldSpace.ToImGui();
                    if (applyHectoPixelScaling)
                        pos = mouseNotification.PositionInWorldSpace.FromHectoToImGui();

                    Trace.WriteLine(pos);
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
                            _io.ClearInputCharacters();
                            _io.ClearInputKeys();
                            break;
                        default:
                            break;
                    }
                }
                else if (notification is TouchNotification touchNotification)
                {
                    // ImGui has no touch - we rely on the mouse emulation of the event system
                }

                foreach (var layer in Layers)
                {
                    layer.Notify(notification, caller);
                }

                return false;
            }
        }

        static ImGuiKey ToImGuiKey(Keys key)
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
