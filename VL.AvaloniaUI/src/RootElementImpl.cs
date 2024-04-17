using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Skia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VL.Lib.IO.Notifications;
using VL.Skia;

namespace VL.AvaloniaUI
{
    sealed class RootElementImpl : ITopLevelImpl
    {
        private readonly Stopwatch _st = Stopwatch.StartNew();
        private readonly List<CallerInfo> callerInfos = new List<CallerInfo>();
        private double scaling = 1;
        private Size clientSize;
        private RawInputModifiers modifiers;

        public RootElementImpl()
        {
            MouseDevice = new MouseDevice();
            KeyboardDevice = AvaloniaLocator.Current.GetService<IKeyboardDevice>();
        }

        public Size ClientSize
        {
            get { return clientSize; }
            set
            {
                if (value != clientSize)
                {
                    clientSize = value;
                    Resized?.Invoke(value, PlatformResizeReason.Unspecified);
                }
            }
        }

        public Size? FrameSize => clientSize;

        public double RenderScaling
        {
            get { return scaling; }
            set
            {
                scaling = value;
                ScalingChanged?.Invoke(value);
            }
        }

        public IEnumerable<object> Surfaces => callerInfos;

        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size, PlatformResizeReason> Resized { get; set; }
        public Action<double> ScalingChanged { get; set; }

        public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }

        public Action Closed { get; set; }
        public Action LostFocus { get; set; }

        public IMouseDevice MouseDevice { get; }

        public IKeyboardDevice KeyboardDevice { get; }

        public IInputRoot InputRoot { get; set; }

        private ulong Timestamp => (ulong)_st.ElapsedMilliseconds;

        public WindowTransparencyLevel TransparencyLevel => WindowTransparencyLevel.None;

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels => throw new NotImplementedException();

        public IPopupImpl CreatePopup()
        {
            return null;
        }

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            return new SkiaImmediateRenderer(root);
        }

        public void Dispose()
        {
        }

        public void Invalidate(Rect rect)
        {
        }

        public Point PointToClient(PixelPoint point) => point.ToPoint(RenderScaling);

        public PixelPoint PointToScreen(Point point) => PixelPoint.FromPoint(point, RenderScaling);

        public void SetCursor(ICursorImpl cursor)
        {

        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
        }

        public void SetTransparencyLevelHint(WindowTransparencyLevel transparencyLevel)
        {
            throw new NotImplementedException();
        }

        internal bool Notify(INotification notification, CallerInfo caller)
        {
            var e = default(RawInputEventArgs);

            if (notification is KeyCodeNotification keyCode)
            {
                modifiers = keyCode.KeyData.ToModifier();
            }
            if (notification is MouseButtonNotification m)
            {
                if (m.Kind == MouseNotificationKind.MouseDown)
                    modifiers |= m.Buttons.ToModifier();
                else if (m.Kind == MouseNotificationKind.MouseUp)
                    modifiers ^= m.Buttons.ToModifier();
            }

            if (notification is KeyDownNotification keyDown)
                Input?.Invoke(e = new RawKeyEventArgs(KeyboardDevice, Timestamp, InputRoot, RawKeyEventType.KeyDown, keyDown.KeyData.ToKey(), modifiers));
            else if (notification is KeyUpNotification keyUp)
                Input?.Invoke(e = new RawKeyEventArgs(KeyboardDevice, Timestamp, InputRoot, RawKeyEventType.KeyUp, keyUp.KeyData.ToKey(), modifiers));
            else if (notification is KeyPressNotification keyPress && !char.IsControl(keyPress.KeyChar))
                Input?.Invoke(e = new RawTextInputEventArgs(KeyboardDevice, Timestamp, InputRoot, keyPress.KeyChar.ToString()));
            else if (notification is MouseDownNotification mouseDown)
                Input?.Invoke(e = new RawPointerEventArgs(MouseDevice, Timestamp, InputRoot, mouseDown.Buttons.ToEventType(false), mouseDown.Position.ToPoint(), modifiers));
            else if (notification is MouseUpNotification mouseUp)
                Input?.Invoke(e = new RawPointerEventArgs(MouseDevice, Timestamp, InputRoot, mouseUp.Buttons.ToEventType(true), mouseUp.Position.ToPoint(), modifiers));
            else if (notification is MouseMoveNotification mouseMove)
                Input?.Invoke(e = new RawPointerEventArgs(MouseDevice, Timestamp, InputRoot, RawPointerEventType.Move, mouseMove.Position.ToPoint(), modifiers));
            else if (notification is MouseWheelNotification mouseWheel)
                Input?.Invoke(e = new RawMouseWheelEventArgs(MouseDevice, Timestamp, InputRoot, mouseWheel.Position.ToPoint(), new Vector(mouseWheel.WheelDelta, 0), modifiers));

            if (e != null)
                return e.Handled;

            return false;
        }

        internal void Render(CallerInfo caller)
        {
            var bounds = caller.ViewportBounds.ToAvaloniaRect();
            ClientSize = bounds.Size;

            callerInfos.Clear();
            callerInfos.Add(caller);

            caller.Canvas.Save();
            try
            {
                Paint?.Invoke(bounds);
            }
            finally
            {
                caller.Canvas.Restore();
            }
        }
    }
}
