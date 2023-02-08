using Stride.Core.Mathematics;
using System;
using System.Drawing;

namespace VL.Lib.IO.Notifications
{
    public static class NotificationHelpers
    {
        public static Vector2 ToVector2(this Size s) => new Vector2(s.Width, s.Height);
        public static Vector2 ToVector2(this SizeF s) => new Vector2(s.Width, s.Height);
        public static Vector2 ToVector2(this Size2F s) => new Vector2(s.Width, s.Height);
        public static Vector2 ToVector2(this System.Drawing.Point p) => new Vector2(p.X, p.Y);

        public static bool IsLeft(this MouseButtons buttons)
        {
            return (buttons & MouseButtons.Left) != 0;
        }

        public static bool IsMiddle(this MouseButtons buttons)
        {
            return (buttons & MouseButtons.Middle) != 0;
        }

        public static bool IsRight(this MouseButtons buttons)
        {
            return (buttons & MouseButtons.Right) != 0;
        }

        public static bool IsXButton1(this MouseButtons buttons)
        {
            return (buttons & MouseButtons.XButton1) != 0;
        }

        public static bool IsXbutton2(this MouseButtons buttons)
        {
            return (buttons & MouseButtons.XButton2) != 0;
        }

        public static bool IsMouseDown(this INotification notification) => (notification as MouseNotification)?.IsMouseDown ?? false;
        public static bool IsMouseUp(this INotification notification) => (notification as MouseNotification)?.IsMouseUp ?? false;
        public static bool IsMouseMove(this INotification notification) => (notification as MouseNotification)?.IsMouseMove ?? false;
        public static bool IsMouseWheel(this INotification notification) => (notification as MouseNotification)?.IsMouseWheel ?? false;
        public static bool IsMouseClick(this INotification notification) => (notification as MouseNotification)?.IsMouseClick ?? false;
        public static bool IsMouseLost(this INotification notification) => (notification as MouseNotification)?.IsDeviceLost ?? false;
        public static bool IsMouseSingleClick(this INotification notification) => (notification as MouseNotification)?.IsMouseSingleClick ?? false;
        public static bool IsMouseDoubleClick(this INotification notification) => (notification as MouseNotification)?.IsMouseDoubleClick ?? false;

        public static TResult NotificationSwitch<TResult>(object eventArg, TResult defaultResult, 
            Func<MouseNotification, TResult> mouseFunc = null,
            Func<KeyNotification, TResult> keyFunc = null,
            Func<TouchNotification, TResult> touchFunc = null,
            Func<GestureNotification, TResult> gestureFunc = null)
        {
            if (mouseFunc != null)
            {
                var mouseNotification = eventArg as MouseNotification;
                if (mouseNotification != null)
                {
                    return mouseFunc(mouseNotification);
                } 
            }

            if (touchFunc != null)
            {
                var touchNotification = eventArg as TouchNotification;
                if (touchNotification != null)
                {
                    return touchFunc(touchNotification);
                } 
            }

            if (keyFunc != null)
            {
                var keyNotification = eventArg as KeyNotification;
                if (keyNotification != null)
                {
                    return keyFunc(keyNotification);
                } 
            }

            if (gestureFunc != null)
            {
                var gestureNotification = eventArg as GestureNotification;
                if (gestureNotification != null)
                {
                    return gestureFunc(gestureNotification);
                } 
            }

            return defaultResult;
        }

        public static TResult MouseNotificationSwitch<TResult>(MouseNotification notification, TResult defaultResult,
            Func<MouseDownNotification, TResult> onDown,
            Func<MouseMoveNotification, TResult> onMove,
            Func<MouseUpNotification, TResult> onUp,
            Func<MouseClickNotification, TResult> onClick = null,
            Func<MouseWheelNotification, TResult> onWheel = null,
            Func<MouseHorizontalWheelNotification, TResult> onHorizontalWheel = null,
            Func<MouseLostNotification, TResult> onDeviceLost = null
            )
        {
            switch (notification.Kind)
            {
                case MouseNotificationKind.MouseDown:
                    return onDown != null ? onDown((MouseDownNotification)notification) : defaultResult;
                case MouseNotificationKind.MouseUp:
                    return onUp != null ? onUp((MouseUpNotification)notification) : defaultResult;
                case MouseNotificationKind.MouseMove:
                    return onMove != null ? onMove((MouseMoveNotification)notification) : defaultResult;
                case MouseNotificationKind.MouseWheel:
                    return onWheel != null? onWheel((MouseWheelNotification)notification) : defaultResult;
                case MouseNotificationKind.MouseHorizontalWheel:
                    return onHorizontalWheel != null ? onHorizontalWheel((MouseHorizontalWheelNotification)notification) : defaultResult;
                case MouseNotificationKind.MouseClick:
                    return onClick != null ? onClick((MouseClickNotification)notification) : defaultResult;
                case MouseNotificationKind.DeviceLost:
                    return onDeviceLost != null ? onDeviceLost((MouseLostNotification)notification) : defaultResult;
                default:
                    return defaultResult;
            }
        }

        public static TResult KeyNotificationSwitch<TResult>(KeyNotification notification, TResult defaultResult,
            Func<KeyDownNotification, TResult> onDown,
            Func<KeyUpNotification, TResult> onUp,
            Func<KeyPressNotification, TResult> onPress,
            Func<KeyboardLostNotification, TResult> onDeviceLost = null)
        {
            switch (notification.Kind)
            {
                case KeyNotificationKind.KeyDown:
                    return onDown != null ? onDown((KeyDownNotification)notification) : defaultResult;
                case KeyNotificationKind.KeyPress:
                    return onPress != null ? onPress((KeyPressNotification)notification) : defaultResult;
                case KeyNotificationKind.KeyUp:
                    return onUp != null ? onUp((KeyUpNotification)notification) : defaultResult;
                case KeyNotificationKind.DeviceLost:
                    return onDeviceLost != null ? onDeviceLost((KeyboardLostNotification)notification) : defaultResult;
                default:
                    return defaultResult;
            }
        }

        public static TResult TouchNotificationSwitch<TResult>(TouchNotification notification, TResult defaultResult,
            Func<TouchNotification, TResult> onDown,
            Func<TouchNotification, TResult> onMove,
            Func<TouchNotification, TResult> onUp)
        {
            switch (notification.Kind)
            {
                case TouchNotificationKind.TouchDown:
                    return onDown != null ? onDown.Invoke((TouchNotification)notification) : defaultResult;
                case TouchNotificationKind.TouchUp:
                    return onUp != null ? onUp.Invoke((TouchNotification)notification) : defaultResult;
                case TouchNotificationKind.TouchMove:
                    return onMove != null ? onMove.Invoke((TouchNotification)notification) : defaultResult;
                default:
                    return defaultResult;
            }
        }

        public static TResult GestureNotificationSwitch<TResult>(GestureNotification notification, TResult defaultResult,
            Func<GestureNotification, TResult> onBegin,
            Func<GestureNotification, TResult> onEnd,
            Func<GestureNotification, TResult> onZoom,
            Func<GestureNotification, TResult> onPan,
            Func<GestureNotification, TResult> onRotate,
            Func<GestureNotification, TResult> onTwoFingerTap,
            Func<GestureNotification, TResult> onPressAndTap)
        {
            switch (notification.Kind)
            {
                case GestureNotificationKind.GestureBegin:
                    return onBegin != null ? onBegin.Invoke((GestureNotification)notification) : defaultResult;
                case GestureNotificationKind.GestureEnd:
                    return onEnd != null? onEnd.Invoke((GestureNotification)notification) : defaultResult;
                case GestureNotificationKind.GestureZoom:
                    return onZoom != null? onZoom.Invoke((GestureNotification)notification) : defaultResult;
                case GestureNotificationKind.GesturePan:
                    return onPan != null? onPan.Invoke((GestureNotification)notification) : defaultResult;
                case GestureNotificationKind.GestureRotate:
                    return onRotate != null? onRotate.Invoke((GestureNotification)notification) : defaultResult;
                case GestureNotificationKind.GestureTwoFingerTap:
                    return onTwoFingerTap != null? onTwoFingerTap.Invoke((GestureNotification)notification) : defaultResult;
                case GestureNotificationKind.GesturePressAndTap:
                    return onPressAndTap != null? onPressAndTap.Invoke((GestureNotification)notification) : defaultResult;
                default:
                    return defaultResult;
            }
        }
    }
}
