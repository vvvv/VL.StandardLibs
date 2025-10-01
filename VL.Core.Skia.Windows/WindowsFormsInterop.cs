using VL.Lib.IO.Notifications;

namespace System.Windows.Forms
{
    public static class WindowsFormsInterop
    {
        public static MouseMoveNotification ToMouseMoveNotification(this MouseEventArgs args, Control relativeTo, object sender = null)
        {
            return new MouseMoveNotification(args.Location.ToVector2(), relativeTo.ClientSize.ToVector2(), Control.ModifierKeys.ToOurs(), sender);
        }

        public static MouseDownNotification ToMouseDownNotification(this MouseEventArgs args, Control relativeTo, object sender = null)
        {
            return new MouseDownNotification(args.Location.ToVector2(), relativeTo.ClientSize.ToVector2(), args.Button.ToOurs(), Control.ModifierKeys.ToOurs(), sender);
        }

        public static MouseUpNotification ToMouseUpNotification(this MouseEventArgs args, Control relativeTo, object sender = null)
        {
            return new MouseUpNotification(args.Location.ToVector2(), relativeTo.ClientSize.ToVector2(), args.Button.ToOurs(), Control.ModifierKeys.ToOurs(), sender);
        }

        public static MouseWheelNotification ToMouseWheelNotification(this MouseEventArgs args, Control relativeTo, object sender = null)
        {
            return new MouseWheelNotification(args.Location.ToVector2(), relativeTo.ClientSize.ToVector2(), args.Delta, Control.ModifierKeys.ToOurs(), sender);
        }

        public static MouseLostNotification ToMouseLostNotification(this EventArgs args, Control relativeTo, object sender = null)
        {
            return new MouseLostNotification(new Stride.Core.Mathematics.Vector2(-100000, -100000), relativeTo.ClientSize.ToVector2(), Control.ModifierKeys.ToOurs(), sender);
        }

        public static GotFocusNotification ToGotFocusNotification(this EventArgs args, Control relativeTo, object sender = null)
        {
            return new GotFocusNotification(sender, Control.ModifierKeys.ToOurs());
        }

        public static LostFocusNotification ToLostFocusNotification(this EventArgs args, Control relativeTo, object sender = null)
        {
            return new LostFocusNotification(sender, Control.ModifierKeys.ToOurs());
        }

        public static KeyDownNotification ToKeyDownNotification(this KeyEventArgs eventArgs, object sender = null)
        {
            return new KeyDownNotification(eventArgs.KeyCode.ToOurs(), Control.ModifierKeys.ToOurs(), sender);
        }

        public static KeyUpNotification ToKeyUpNotification(this KeyEventArgs eventArgs, object sender = null)
        {
            return new KeyUpNotification(eventArgs.KeyCode.ToOurs(), Control.ModifierKeys.ToOurs(), sender);
        }

        public static KeyPressNotification ToKeyPressNotification(this KeyPressEventArgs eventArgs, object sender = null)
        {
            return new KeyPressNotification(eventArgs.KeyChar, Control.ModifierKeys.ToOurs(), sender);
        }

        public static MouseButtons ToNative(this VL.Lib.IO.MouseButtons keys) => (MouseButtons)keys;

        public static Keys ToNative(this VL.Lib.IO.Keys keys) => (Keys)keys;

        public static VL.Lib.IO.MouseButtons ToOurs(this MouseButtons keys) => (VL.Lib.IO.MouseButtons)keys;

        public static VL.Lib.IO.Keys ToOurs(this Keys keys) => (VL.Lib.IO.Keys)keys;
    }
}
