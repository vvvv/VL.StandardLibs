using VL.Lib.IO.Notifications;

namespace System.Windows.Forms
{
    public static class WindowsFormsInterop
    {
        public static MouseMoveNotification ToMouseMoveNotification(this MouseEventArgs args, Control relativeTo, object sender = null, bool inScreenSpace = false)
        {
            var position = inScreenSpace ? relativeTo.PointToScreen(args.Location) : args.Location;
            return new MouseMoveNotification(position.ToVector2(), relativeTo.ClientSize.ToVector2(), Control.ModifierKeys.ToOurs(), sender);
        }

        public static MouseDownNotification ToMouseDownNotification(this MouseEventArgs args, Control relativeTo, object sender = null, bool inScreenSpace = false)
        {
            var position = inScreenSpace ? relativeTo.PointToScreen(args.Location) : args.Location;
            return new MouseDownNotification(position.ToVector2(), relativeTo.ClientSize.ToVector2(), args.Button.ToOurs(), Control.ModifierKeys.ToOurs(), sender);
        }

        public static MouseUpNotification ToMouseUpNotification(this MouseEventArgs args, Control relativeTo, object sender = null, bool inScreenSpace = false)
        {
            var position = inScreenSpace ? relativeTo.PointToScreen(args.Location) : args.Location;
            return new MouseUpNotification(position.ToVector2(), relativeTo.ClientSize.ToVector2(), args.Button.ToOurs(), Control.ModifierKeys.ToOurs(), sender);
        }

        public static MouseWheelNotification ToMouseWheelNotification(this MouseEventArgs args, Control relativeTo, object sender = null, bool inScreenSpace = false)
        {
            var position = inScreenSpace ? relativeTo.PointToScreen(args.Location) : args.Location;
            return new MouseWheelNotification(position.ToVector2(), relativeTo.ClientSize.ToVector2(), args.Delta, Control.ModifierKeys.ToOurs(), sender);
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
