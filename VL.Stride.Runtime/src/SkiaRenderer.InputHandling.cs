using SkiaSharp;
using Stride.Input;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;
using VL.Skia;
using StrideKeys = Stride.Input.Keys;
using VLKeys = VL.Lib.IO.Keys;

namespace VL.Stride
{
    partial class SkiaRenderer
    {
        IDisposable SubscribeToInputSource(IInputSource inputSource, RenderDrawContext context, SKCanvas canvas, GRContext graphicsContext)
        {
            if (inputSource is null)
                return Disposable.Empty;

            var inputManager = context.RenderContext.Services.GetService<InputManager>();
            if (inputManager is null)
                return Disposable.Empty;

            var subscription = new CompositeDisposable();

            var renderTarget = context.CommandList.RenderTarget;
            var callerInfo = CallerInfo.InRenderer(renderTarget.Width, renderTarget.Height, canvas, graphicsContext);

            var mouseButtonListener = NewMouseButtonListener(inputSource, callerInfo, subscription);
            inputManager.AddListener(mouseButtonListener);
            var mouseWheelListener = NewMouseWheelListener(inputSource, callerInfo);
            inputManager.AddListener(mouseWheelListener);
            var pointerListener = NewPointerListener(inputSource, callerInfo);
            inputManager.AddListener(pointerListener);
            var keyListener = NewKeyListener(inputSource, callerInfo);
            inputManager.AddListener(keyListener);
            var textListener = NewTextInputListener(inputSource, callerInfo);
            inputManager.AddListener(textListener);

            subscription.Add(Disposable.Create(() =>
            {
                inputManager.RemoveListener(mouseButtonListener);
                inputManager.RemoveListener(mouseWheelListener);
                inputManager.RemoveListener(pointerListener);
                inputManager.RemoveListener(keyListener);
                inputManager.RemoveListener(textListener);
            }));

            return subscription;
        }

        private IInputEventListener NewPointerListener(IInputSource inputSource, CallerInfo callerInfo)
        {
            return new AnonymousEventListener<PointerEvent>(e =>
            {
                if (e.Device.Source != inputSource || Layer is null)
                    return;

                // Stride uses the "pointer" abstraction. Mouse and touch are treated the same way. For a mouse down we'll receive a pointer pressed as well.
                // One could program against that API and thereby would support both input devices.
                // Our Skia renderer on the other hand uses the Windows default, it first sends touch messages and if they're not handled by the app
                // it will generate mouse (& gesture) messages (https://learn.microsoft.com/en-us/windows/win32/inputmsg/wm-pointerdown).
                // We currently don't support the pointer abstraction, we therefor must tweak the Stride abstraction a little to follow the normal Windows behaviour.

                var pointerDevice = e.Device as IPointerDevice;
                var modifiers = GetModifierKeys(inputSource);
                switch (e.EventType)
                {
                    case PointerEventType.Pressed:
                        // Stride simulates touch events when using the mouse. We don't want these made-up events here.
                        if (e.Device is not IMouseDevice)
                        {
                            var n = new TouchNotification(TouchNotificationKind.TouchDown, e.AbsolutePosition, pointerDevice.SurfaceSize, e.PointerId, primary: e.PointerId == 0, default, default, modifiers, this);
                            Layer.Notify(n, callerInfo);
                            if (!n.Handled)
                                Layer.Notify(new MouseDownNotification(e.Position * pointerDevice.SurfaceSize, pointerDevice.SurfaceSize, MouseButtons.Left, modifiers, this), callerInfo);
                        }
                        break;
                    case PointerEventType.Moved:
                        if (e.Device is IMouseDevice)
                            Layer.Notify(new MouseMoveNotification(e.AbsolutePosition, pointerDevice.SurfaceSize, modifiers, this), callerInfo);
                        else
                        {
                            var n = new TouchNotification(TouchNotificationKind.TouchMove, e.AbsolutePosition, pointerDevice.SurfaceSize, e.PointerId, primary: e.PointerId == 0, default, default, modifiers, this);
                            Layer.Notify(n, callerInfo);
                            if (!n.Handled)
                                Layer.Notify(new MouseMoveNotification(e.Position * pointerDevice.SurfaceSize, pointerDevice.SurfaceSize, modifiers, this), callerInfo);
                        }
                        break;
                    case PointerEventType.Released:
                        // Stride simulates touch events when using the mouse. We don't want these made-up events here.
                        if (e.Device is not IMouseDevice)
                        {
                            var n = new TouchNotification(TouchNotificationKind.TouchUp, e.AbsolutePosition, pointerDevice.SurfaceSize, e.PointerId, primary: e.PointerId == 0, default, default, modifiers, this);
                            Layer.Notify(n, callerInfo);
                            if (!n.Handled)
                                Layer.Notify(new MouseUpNotification(e.Position * pointerDevice.SurfaceSize, pointerDevice.SurfaceSize, MouseButtons.Left, modifiers, this), callerInfo);
                        }
                        break;
                    case PointerEventType.Canceled:
                        break;
                    default:
                        break;
                }
            });
        }

        private IInputEventListener NewMouseButtonListener(IInputSource inputSource, CallerInfo callerInfo, CompositeDisposable subscription)
        {
            // We don't receive any click events from Stride - so let's use our existing emulation code for that (tested since beta times)
            var eventStream = new Subject<MouseNotification>().DisposeBy(subscription);
            var mouse = new VL.Lib.IO.Mouse(eventStream, injectMouseClicks: true, keepSingleSubscription: false);
            mouse.Notifications.Subscribe(n => Layer.Notify(n, callerInfo)).DisposeBy(subscription);
            return new AnonymousEventListener<MouseButtonEvent>(e =>
            {
                if (e.Device.Source != inputSource || Layer is null)
                    return;

                var mouseDevice = e.Device as IMouseDevice;
                var modifiers = GetModifierKeys(inputSource);
                if (e.IsDown)
                    eventStream.OnNext(new MouseDownNotification(mouseDevice.Position * mouseDevice.SurfaceSize, mouseDevice.SurfaceSize, ToWinFormsButton(e.Button), modifiers, this));
                else
                    eventStream.OnNext(new MouseUpNotification(mouseDevice.Position * mouseDevice.SurfaceSize, mouseDevice.SurfaceSize, ToWinFormsButton(e.Button), modifiers, this));
            });
        }

        private IInputEventListener NewMouseWheelListener(IInputSource inputSource, CallerInfo callerInfo)
        {
            return new AnonymousEventListener<MouseWheelEvent>(e =>
            {
                if (e.Device.Source != inputSource || Layer is null)
                    return;

                var mouseDevice = e.Device as IMouseDevice;
                var modifiers = GetModifierKeys(inputSource);
                Layer.Notify(new MouseWheelNotification(mouseDevice.Position * mouseDevice.SurfaceSize, mouseDevice.SurfaceSize, (int)e.WheelDelta * 120 /* Same as in original winforms renderer */, modifiers, this), callerInfo);
            });
        }

        private IInputEventListener NewKeyListener(IInputSource inputSource, CallerInfo callerInfo)
        {
            return new AnonymousEventListener<KeyEvent>(e =>
            {
                if (e.Device.Source != inputSource || Layer is null)
                    return;

                var key = ToVLKeys(e.Key);
                var keyBoard = e.Device as IKeyboardDevice;
                var modifierKeys = GetModifierKeys(keyBoard);
                if (e.IsDown)
                    Layer.Notify(new KeyDownNotification(key, modifierKeys, this), callerInfo);
                else
                    Layer.Notify(new KeyUpNotification(key, modifierKeys, this), callerInfo);
            });
        }

        private IInputEventListener NewTextInputListener(IInputSource inputSource, CallerInfo callerInfo)
        {
            return new AnonymousEventListener<TextInputEvent>(e =>
            {
                if (e.Device.Source != inputSource || Layer is null)
                    return;

                if (e.Type == TextInputEventType.Input)
                {
                    var modifiers = GetModifierKeys(inputSource);
                    foreach (var c in e.Text)
                        Layer.Notify(new KeyPressNotification(c, modifiers, this), callerInfo);
                }
            });
        }

        static MouseButtons ToWinFormsButton(MouseButton mouseButton)
        {
            switch (mouseButton)
            {
                case MouseButton.Left:
                    return MouseButtons.Left;
                case MouseButton.Middle:
                    return MouseButtons.Middle;
                case MouseButton.Right:
                    return MouseButtons.Right;
                case MouseButton.Extended1:
                    return MouseButtons.XButton1;
                case MouseButton.Extended2:
                    return MouseButtons.XButton2;
                default:
                    return MouseButtons.None;
            }
        }

        static VLKeys ToVLKeys(StrideKeys keys)
        {
            // The original Skia renderer doesn't differentiate between left and right keys
            switch (keys)
            {
                case StrideKeys.LeftShift:
                case StrideKeys.RightShift:
                    return VLKeys.ShiftKey;
                case StrideKeys.LeftCtrl:
                case StrideKeys.RightCtrl:
                    return VLKeys.ControlKey;
                case StrideKeys.LeftAlt:
                case StrideKeys.RightAlt:
                    return VLKeys.Menu;
            }
            if (WinKeys.ReverseMapKeys.TryGetValue(keys, out var value))
                return value;
            else
                return VLKeys.None;
        }

        static VLKeys GetModifierKeys(IInputSource inputSource)
        {
            foreach (var entry in inputSource.Devices)
                if (entry.Value is IKeyboardDevice keyboard)
                    return GetModifierKeys(keyboard);
            return VLKeys.None;
        }

        static VLKeys GetModifierKeys(IKeyboardDevice keyBoard)
        {
            var modifierKeys = VLKeys.None;
            if (keyBoard.DownKeys.Contains(StrideKeys.LeftShift) || keyBoard.DownKeys.Contains(StrideKeys.RightShift))
                modifierKeys |= VLKeys.Shift;
            if (keyBoard.DownKeys.Contains(StrideKeys.LeftCtrl) || keyBoard.DownKeys.Contains(StrideKeys.RightCtrl))
                modifierKeys |= VLKeys.Control;
            if (keyBoard.DownKeys.Contains(StrideKeys.LeftAlt) || keyBoard.DownKeys.Contains(StrideKeys.RightAlt))
                modifierKeys |= VLKeys.Alt;
            return modifierKeys;
        }

        sealed class AnonymousEventListener<T> : IInputEventListener<T> 
            where T : InputEvent
        {
            readonly Action<T> ProcessEventAction;

            public AnonymousEventListener(Action<T> processEventAction)
            {
                ProcessEventAction = processEventAction;
            }

            public void ProcessEvent(T inputEvent)
            {
                ProcessEventAction(inputEvent);
            }
        }

        internal static class WinKeys
        {
            /// <summary>
            /// Map between Winform keys and Stride keys.
            /// </summary>
            internal static readonly Dictionary<VLKeys, StrideKeys> MapKeys = NewMapKeys();

            /// <summary>
            /// Map between Stride keys and Winforms keys.
            /// </summary>
            internal static readonly Dictionary<StrideKeys, VLKeys> ReverseMapKeys = NewMapKeys().ToDictionary(e => e.Value, e => e.Key);


            private static Dictionary<VLKeys, StrideKeys> NewMapKeys()
            {
                var map = new Dictionary<VLKeys, StrideKeys>(200);
                map[VLKeys.None] = StrideKeys.None;
                map[VLKeys.Cancel] = StrideKeys.Cancel;
                map[VLKeys.Back] = StrideKeys.Back;
                map[VLKeys.Tab] = StrideKeys.Tab;
                map[VLKeys.LineFeed] = StrideKeys.LineFeed;
                map[VLKeys.Clear] = StrideKeys.Clear;
                map[VLKeys.Enter] = StrideKeys.Enter;
                map[VLKeys.Return] = StrideKeys.Return;
                map[VLKeys.Pause] = StrideKeys.Pause;
                map[VLKeys.Capital] = StrideKeys.Capital;
                map[VLKeys.CapsLock] = StrideKeys.CapsLock;
                map[VLKeys.HangulMode] = StrideKeys.HangulMode;
                map[VLKeys.KanaMode] = StrideKeys.KanaMode;
                map[VLKeys.JunjaMode] = StrideKeys.JunjaMode;
                map[VLKeys.FinalMode] = StrideKeys.FinalMode;
                map[VLKeys.HanjaMode] = StrideKeys.HanjaMode;
                map[VLKeys.KanjiMode] = StrideKeys.KanjiMode;
                map[VLKeys.Escape] = StrideKeys.Escape;
                map[VLKeys.IMEConvert] = StrideKeys.ImeConvert;
                map[VLKeys.IMENonconvert] = StrideKeys.ImeNonConvert;
                map[VLKeys.IMEAccept] = StrideKeys.ImeAccept;
                map[VLKeys.IMEModeChange] = StrideKeys.ImeModeChange;
                map[VLKeys.Space] = StrideKeys.Space;
                map[VLKeys.PageUp] = StrideKeys.PageUp;
                map[VLKeys.Prior] = StrideKeys.Prior;
                map[VLKeys.Next] = StrideKeys.Next;
                map[VLKeys.PageDown] = StrideKeys.PageDown;
                map[VLKeys.End] = StrideKeys.End;
                map[VLKeys.Home] = StrideKeys.Home;
                map[VLKeys.Left] = StrideKeys.Left;
                map[VLKeys.Up] = StrideKeys.Up;
                map[VLKeys.Right] = StrideKeys.Right;
                map[VLKeys.Down] = StrideKeys.Down;
                map[VLKeys.Select] = StrideKeys.Select;
                map[VLKeys.Print] = StrideKeys.Print;
                map[VLKeys.Execute] = StrideKeys.Execute;
                map[VLKeys.PrintScreen] = StrideKeys.PrintScreen;
                map[VLKeys.Snapshot] = StrideKeys.Snapshot;
                map[VLKeys.Insert] = StrideKeys.Insert;
                map[VLKeys.Delete] = StrideKeys.Delete;
                map[VLKeys.Help] = StrideKeys.Help;
                map[VLKeys.D0] = StrideKeys.D0;
                map[VLKeys.D1] = StrideKeys.D1;
                map[VLKeys.D2] = StrideKeys.D2;
                map[VLKeys.D3] = StrideKeys.D3;
                map[VLKeys.D4] = StrideKeys.D4;
                map[VLKeys.D5] = StrideKeys.D5;
                map[VLKeys.D6] = StrideKeys.D6;
                map[VLKeys.D7] = StrideKeys.D7;
                map[VLKeys.D8] = StrideKeys.D8;
                map[VLKeys.D9] = StrideKeys.D9;
                map[VLKeys.A] = StrideKeys.A;
                map[VLKeys.B] = StrideKeys.B;
                map[VLKeys.C] = StrideKeys.C;
                map[VLKeys.D] = StrideKeys.D;
                map[VLKeys.E] = StrideKeys.E;
                map[VLKeys.F] = StrideKeys.F;
                map[VLKeys.G] = StrideKeys.G;
                map[VLKeys.H] = StrideKeys.H;
                map[VLKeys.I] = StrideKeys.I;
                map[VLKeys.J] = StrideKeys.J;
                map[VLKeys.K] = StrideKeys.K;
                map[VLKeys.L] = StrideKeys.L;
                map[VLKeys.M] = StrideKeys.M;
                map[VLKeys.N] = StrideKeys.N;
                map[VLKeys.O] = StrideKeys.O;
                map[VLKeys.P] = StrideKeys.P;
                map[VLKeys.Q] = StrideKeys.Q;
                map[VLKeys.R] = StrideKeys.R;
                map[VLKeys.S] = StrideKeys.S;
                map[VLKeys.T] = StrideKeys.T;
                map[VLKeys.U] = StrideKeys.U;
                map[VLKeys.V] = StrideKeys.V;
                map[VLKeys.W] = StrideKeys.W;
                map[VLKeys.X] = StrideKeys.X;
                map[VLKeys.Y] = StrideKeys.Y;
                map[VLKeys.Z] = StrideKeys.Z;
                map[VLKeys.LWin] = StrideKeys.LeftWin;
                map[VLKeys.RWin] = StrideKeys.RightWin;
                map[VLKeys.Apps] = StrideKeys.Apps;
                map[VLKeys.Sleep] = StrideKeys.Sleep;
                map[VLKeys.NumPad0] = StrideKeys.NumPad0;
                map[VLKeys.NumPad1] = StrideKeys.NumPad1;
                map[VLKeys.NumPad2] = StrideKeys.NumPad2;
                map[VLKeys.NumPad3] = StrideKeys.NumPad3;
                map[VLKeys.NumPad4] = StrideKeys.NumPad4;
                map[VLKeys.NumPad5] = StrideKeys.NumPad5;
                map[VLKeys.NumPad6] = StrideKeys.NumPad6;
                map[VLKeys.NumPad7] = StrideKeys.NumPad7;
                map[VLKeys.NumPad8] = StrideKeys.NumPad8;
                map[VLKeys.NumPad9] = StrideKeys.NumPad9;
                map[VLKeys.Multiply] = StrideKeys.Multiply;
                map[VLKeys.Add] = StrideKeys.Add;
                map[VLKeys.Separator] = StrideKeys.Separator;
                map[VLKeys.Subtract] = StrideKeys.Subtract;
                map[VLKeys.Decimal] = StrideKeys.Decimal;
                map[VLKeys.Divide] = StrideKeys.Divide;
                map[VLKeys.F1] = StrideKeys.F1;
                map[VLKeys.F2] = StrideKeys.F2;
                map[VLKeys.F3] = StrideKeys.F3;
                map[VLKeys.F4] = StrideKeys.F4;
                map[VLKeys.F5] = StrideKeys.F5;
                map[VLKeys.F6] = StrideKeys.F6;
                map[VLKeys.F7] = StrideKeys.F7;
                map[VLKeys.F8] = StrideKeys.F8;
                map[VLKeys.F9] = StrideKeys.F9;
                map[VLKeys.F10] = StrideKeys.F10;
                map[VLKeys.F11] = StrideKeys.F11;
                map[VLKeys.F12] = StrideKeys.F12;
                map[VLKeys.F13] = StrideKeys.F13;
                map[VLKeys.F14] = StrideKeys.F14;
                map[VLKeys.F15] = StrideKeys.F15;
                map[VLKeys.F16] = StrideKeys.F16;
                map[VLKeys.F17] = StrideKeys.F17;
                map[VLKeys.F18] = StrideKeys.F18;
                map[VLKeys.F19] = StrideKeys.F19;
                map[VLKeys.F20] = StrideKeys.F20;
                map[VLKeys.F21] = StrideKeys.F21;
                map[VLKeys.F22] = StrideKeys.F22;
                map[VLKeys.F23] = StrideKeys.F23;
                map[VLKeys.F24] = StrideKeys.F24;
                map[VLKeys.NumLock] = StrideKeys.NumLock;
                map[VLKeys.Scroll] = StrideKeys.Scroll;
                map[VLKeys.LShiftKey] = StrideKeys.LeftShift;
                map[VLKeys.RShiftKey] = StrideKeys.RightShift;
                map[VLKeys.LControlKey] = StrideKeys.LeftCtrl;
                map[VLKeys.RControlKey] = StrideKeys.RightCtrl;
                map[VLKeys.LMenu] = StrideKeys.LeftAlt;
                map[VLKeys.RMenu] = StrideKeys.RightAlt;
                map[VLKeys.BrowserBack] = StrideKeys.BrowserBack;
                map[VLKeys.BrowserForward] = StrideKeys.BrowserForward;
                map[VLKeys.BrowserRefresh] = StrideKeys.BrowserRefresh;
                map[VLKeys.BrowserStop] = StrideKeys.BrowserStop;
                map[VLKeys.BrowserSearch] = StrideKeys.BrowserSearch;
                map[VLKeys.BrowserFavorites] = StrideKeys.BrowserFavorites;
                map[VLKeys.BrowserHome] = StrideKeys.BrowserHome;
                map[VLKeys.VolumeMute] = StrideKeys.VolumeMute;
                map[VLKeys.VolumeDown] = StrideKeys.VolumeDown;
                map[VLKeys.VolumeUp] = StrideKeys.VolumeUp;
                map[VLKeys.MediaNextTrack] = StrideKeys.MediaNextTrack;
                map[VLKeys.MediaPreviousTrack] = StrideKeys.MediaPreviousTrack;
                map[VLKeys.MediaStop] = StrideKeys.MediaStop;
                map[VLKeys.MediaPlayPause] = StrideKeys.MediaPlayPause;
                map[VLKeys.LaunchMail] = StrideKeys.LaunchMail;
                map[VLKeys.SelectMedia] = StrideKeys.SelectMedia;
                map[VLKeys.LaunchApplication1] = StrideKeys.LaunchApplication1;
                map[VLKeys.LaunchApplication2] = StrideKeys.LaunchApplication2;
                map[VLKeys.Oem1] = StrideKeys.Oem1;
                map[VLKeys.OemSemicolon] = StrideKeys.OemSemicolon;
                map[VLKeys.Oemplus] = StrideKeys.OemPlus;
                map[VLKeys.Oemcomma] = StrideKeys.OemComma;
                map[VLKeys.OemMinus] = StrideKeys.OemMinus;
                map[VLKeys.OemPeriod] = StrideKeys.OemPeriod;
                map[VLKeys.Oem2] = StrideKeys.Oem2;
                map[VLKeys.OemQuestion] = StrideKeys.OemQuestion;
                map[VLKeys.Oem3] = StrideKeys.Oem3;
                map[VLKeys.Oemtilde] = StrideKeys.OemTilde;
                map[VLKeys.Oem4] = StrideKeys.Oem4;
                map[VLKeys.OemOpenBrackets] = StrideKeys.OemOpenBrackets;
                map[VLKeys.Oem5] = StrideKeys.Oem5;
                map[VLKeys.OemPipe] = StrideKeys.OemPipe;
                map[VLKeys.Oem6] = StrideKeys.Oem6;
                map[VLKeys.OemCloseBrackets] = StrideKeys.OemCloseBrackets;
                map[VLKeys.Oem7] = StrideKeys.Oem7;
                map[VLKeys.OemQuotes] = StrideKeys.OemQuotes;
                map[VLKeys.Oem8] = StrideKeys.Oem8;
                map[VLKeys.Oem102] = StrideKeys.Oem102;
                map[VLKeys.OemBackslash] = StrideKeys.OemBackslash;
                map[VLKeys.Attn] = StrideKeys.Attn;
                map[VLKeys.Crsel] = StrideKeys.CrSel;
                map[VLKeys.Exsel] = StrideKeys.ExSel;
                map[VLKeys.EraseEof] = StrideKeys.EraseEof;
                map[VLKeys.Play] = StrideKeys.Play;
                map[VLKeys.Zoom] = StrideKeys.Zoom;
                map[VLKeys.NoName] = StrideKeys.NoName;
                map[VLKeys.Pa1] = StrideKeys.Pa1;
                map[VLKeys.OemClear] = StrideKeys.OemClear;
                return map;
            }
        }
    }
}
