using System;

namespace VL.Lib.IO.Notifications
{
    public enum KeyNotificationKind
    {
        KeyDown,
        KeyPress,
        KeyUp,
        DeviceLost
    }

    public abstract class KeyNotification : NotificationBase
    {
        public readonly KeyNotificationKind Kind;
        protected KeyNotification(KeyNotificationKind kind, Keys modifierKeys, object sender)
            : base(sender, modifierKeys)
        {
            Kind = kind;
        }
        public bool IsKeyDown { get { return Kind == KeyNotificationKind.KeyDown; } }
        public bool IsKeyUp { get { return Kind == KeyNotificationKind.KeyUp; } }
        public bool IsKeyPress { get { return Kind == KeyNotificationKind.KeyPress; } }
        public bool IsDeviceLost { get { return Kind == KeyNotificationKind.DeviceLost; } }

        public override INotification Transform(INotificationSpaceTransformer transformer) => this;
    }

    public abstract class KeyCodeNotification : KeyNotification
    {
        public readonly Keys KeyCode;

        public KeyCodeNotification(KeyNotificationKind kind, Keys keyCode, Keys modifierKeys, object sender)
            : base(kind, modifierKeys, sender)
        {
            KeyCode = keyCode;
        }

        public Keys KeyData => KeyCode | ModifierKeys;
    }

    public class KeyDownNotification : KeyCodeNotification
    {
        public KeyDownNotification(Keys keyCode, Keys modifierKeys, object sender)
            : base(KeyNotificationKind.KeyDown, keyCode, modifierKeys, sender)
        {
        }

        public override INotification WithSender(object sender)
            => new KeyDownNotification(KeyCode, ModifierKeys, sender);
    }

    public class KeyPressNotification : KeyNotification
    {
        public readonly char KeyChar;
        public KeyPressNotification(char keyChar, Keys modifierKeys, object sender)
            : base(KeyNotificationKind.KeyPress, modifierKeys, sender)
        {
            KeyChar = keyChar;
        }

        public override INotification WithSender(object sender)
            => new KeyPressNotification(KeyChar, ModifierKeys, sender);
    }

    public class KeyUpNotification : KeyCodeNotification
    {
        public KeyUpNotification(Keys keyCode, Keys modifierKeys, object sender)
            : base(KeyNotificationKind.KeyUp, keyCode, modifierKeys, sender)
        {
        }

        public override INotification WithSender(object sender)
            => new KeyUpNotification(KeyCode, ModifierKeys, sender);
    }

    public class KeyboardLostNotification : KeyNotification
    {
        public KeyboardLostNotification(object sender)
            : base(KeyNotificationKind.DeviceLost, Keys.None, sender)
        {
        }

        public override INotification WithSender(object sender)
            => new KeyboardLostNotification(sender);
    }
}
