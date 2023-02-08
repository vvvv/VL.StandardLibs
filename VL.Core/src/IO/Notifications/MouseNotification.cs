using System;
using Stride.Core.Mathematics;

namespace VL.Lib.IO.Notifications
{
    public enum MouseNotificationKind
    {
        MouseDown,
        MouseUp,
        MouseMove,
        MouseWheel,
        MouseHorizontalWheel,
        MouseClick,
        DeviceLost
    }

    public abstract class MouseNotification : NotificationWithPosition
    {
        public readonly MouseNotificationKind Kind;

        public MouseNotification(MouseNotificationKind kind, Vector2 position, Vector2 clientArea, Keys modifierKeys, object sender)
            : base(position, clientArea, modifierKeys, sender)
        {
            Kind = kind;
        }

        internal bool IsMouseDown { get { return Kind == MouseNotificationKind.MouseDown; } }
        internal bool IsMouseUp { get { return Kind == MouseNotificationKind.MouseUp; } }
        internal bool IsMouseMove { get { return Kind == MouseNotificationKind.MouseMove; } }
        internal bool IsMouseWheel { get { return Kind == MouseNotificationKind.MouseWheel; } }
        internal bool IsMouseClick { get { return Kind == MouseNotificationKind.MouseClick; } }
        internal bool IsDeviceLost { get { return Kind == MouseNotificationKind.DeviceLost; } }
        internal bool IsMouseSingleClick => IsMouseClick && ((MouseClickNotification)this).ClickCount == 1;
        internal bool IsMouseDoubleClick => IsMouseClick && ((MouseClickNotification)this).ClickCount == 2;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this))
                return true;
            var n = obj as MouseNotification;
            if (n != null)
                return n.Kind == Kind && 
                       n.Position == Position && 
                       n.ClientArea == ClientArea;
            return false;
        }

        public override int GetHashCode()
        {
            return Kind.GetHashCode() ^ Position.GetHashCode() ^ ClientArea.GetHashCode();
        }
    }

    public class MouseMoveNotification : MouseNotification
    {
        public MouseMoveNotification(Vector2 position, Vector2 clientArea, Keys modifierKeys, object sender)
            : base(MouseNotificationKind.MouseMove, position, clientArea, modifierKeys, sender)
        {
        }

        public override INotification WithSender(object sender)
            => new MouseMoveNotification(Position, ClientArea, ModifierKeys, sender);

        public override INotification Transform(INotificationSpaceTransformer transformer)
            => new MouseMoveNotification(transformer.TransformPosition(Position), transformer.TransformSize(ClientArea), ModifierKeys, Sender);
    }

    public abstract class MouseButtonNotification : MouseNotification
    {
        public readonly MouseButtons Buttons;
        public MouseButtonNotification(MouseNotificationKind kind, Vector2 position, Vector2 clientArea, MouseButtons buttons, Keys modifierKeys, object sender)
            : base(kind, position, clientArea, modifierKeys, sender)
        {
            Buttons = buttons;
        }

        public override bool Equals(object obj)
        {
            if (base.Equals(obj))
            {
                var n = obj as MouseButtonNotification;
                if (n != null)
                    return n.Buttons == Buttons;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ Buttons.GetHashCode();
        }
    }

    public class MouseDownNotification : MouseButtonNotification
    {
        public MouseDownNotification(Vector2 position, Vector2 clientArea, MouseButtons buttons, Keys modifierKeys, object sender)
            : base(MouseNotificationKind.MouseDown, position, clientArea, buttons, modifierKeys, sender)
        {
        }

        public override INotification WithSender(object sender)
            => new MouseDownNotification(Position, ClientArea, Buttons, ModifierKeys, sender);

        public override INotification Transform(INotificationSpaceTransformer transformer)
            => new MouseDownNotification(transformer.TransformPosition(Position), transformer.TransformSize(ClientArea), Buttons, ModifierKeys, Sender);
    }

    public class MouseUpNotification : MouseButtonNotification
    {
        public MouseUpNotification(Vector2 position, Vector2 clientArea, MouseButtons buttons, Keys modifierKeys, object sender)
            : base(MouseNotificationKind.MouseUp, position, clientArea, buttons, modifierKeys, sender)
        {
        }

        public override INotification WithSender(object sender)
            => new MouseUpNotification(Position, ClientArea, Buttons, ModifierKeys, sender);

        public override INotification Transform(INotificationSpaceTransformer transformer)
            => new MouseUpNotification(transformer.TransformPosition(Position), transformer.TransformSize(ClientArea), Buttons, ModifierKeys, Sender);
    }

    public class MouseClickNotification : MouseButtonNotification
    {
        public readonly int ClickCount;
        public MouseClickNotification(Vector2 position, Vector2 clientArea, MouseButtons buttons, int clickCount, Keys modifierKeys, object sender)
            : base(MouseNotificationKind.MouseClick, position, clientArea, buttons, modifierKeys, sender)
        {
            ClickCount = clickCount;
        }

        public override bool Equals(object obj)
        {
            if (base.Equals(obj))
            {
                var n = obj as MouseClickNotification;
                if (n != null)
                    return n.ClickCount == ClickCount;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ ClickCount.GetHashCode();
        }

        public override INotification WithSender(object sender)
            => new MouseClickNotification(Position, ClientArea, Buttons, ClickCount, ModifierKeys, sender);

        public override INotification Transform(INotificationSpaceTransformer transformer)
            => new MouseClickNotification(transformer.TransformPosition(Position), transformer.TransformSize(ClientArea), Buttons, ClickCount, ModifierKeys, Sender);
    }

    public class MouseWheelNotification : MouseNotification
    {
        public readonly int WheelDelta;

        public MouseWheelNotification(Vector2 position, Vector2 clientArea, int wheelDelta, Keys modifierKeys, object sender)
            : base(MouseNotificationKind.MouseWheel, position, clientArea, modifierKeys, sender)
        {
            WheelDelta = wheelDelta;
        }

        public override bool Equals(object obj)
        {
            if (base.Equals(obj))
            {
                var n = obj as MouseWheelNotification;
                if (n != null)
                    return n.WheelDelta == WheelDelta;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ WheelDelta.GetHashCode();
        }

        public override INotification WithSender(object sender)
            => new MouseWheelNotification(Position, ClientArea, WheelDelta, ModifierKeys, sender);

        public override INotification Transform(INotificationSpaceTransformer transformer)
            => new MouseWheelNotification(transformer.TransformPosition(Position), transformer.TransformSize(ClientArea), WheelDelta, ModifierKeys, Sender);
    }

    public class MouseHorizontalWheelNotification : MouseNotification
    {
        public readonly int WheelDelta;

        public MouseHorizontalWheelNotification(Vector2 position, Vector2 clientArea, int wheelDelta, Keys modifierKeys, object sender)
            : base(MouseNotificationKind.MouseHorizontalWheel, position, clientArea, modifierKeys, sender)
        {
            WheelDelta = wheelDelta;
        }

        public override bool Equals(object obj)
        {
            if (base.Equals(obj))
            {
                var n = obj as MouseHorizontalWheelNotification;
                if (n != null)
                    return n.WheelDelta == WheelDelta;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ WheelDelta.GetHashCode();
        }

        public override INotification WithSender(object sender)
            => new MouseHorizontalWheelNotification(Position, ClientArea, WheelDelta, ModifierKeys, sender);

        public override INotification Transform(INotificationSpaceTransformer transformer)
            => new MouseHorizontalWheelNotification(transformer.TransformPosition(Position), transformer.TransformSize(ClientArea), WheelDelta, ModifierKeys, Sender);
    }

    public class MouseLostNotification : MouseNotification
    {
        public MouseLostNotification(Vector2 position, Vector2 clientArea, Keys modifierKeys, object sender)
            : base(MouseNotificationKind.DeviceLost, position, clientArea, modifierKeys, sender)
        {
        }

        public override INotification WithSender(object sender)
            => new MouseLostNotification(Position, ClientArea, ModifierKeys, sender);

        public override INotification Transform(INotificationSpaceTransformer transformer)
            => new MouseLostNotification(transformer.TransformPosition(Position), transformer.TransformSize(ClientArea), ModifierKeys, Sender);
    }
}
