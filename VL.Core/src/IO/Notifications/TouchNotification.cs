using System;
using Stride.Core.Mathematics;

namespace VL.Lib.IO.Notifications
{
    public enum TouchNotificationKind
    {
        TouchDown,
        TouchUp,
        TouchMove
    }

    public class TouchNotification : NotificationWithPosition
    {
        public readonly TouchNotificationKind Kind;
        public readonly int Id;
        public readonly bool Primary;
        public readonly Vector2 ContactArea;
        public readonly long TouchDeviceID;

        /// <summary>
        /// Whether the contact was lost instead of lifted: the system took it away, or the window went away.
        /// Only set on a TouchUp. Who reacts to a completed touch (a tap) should ignore a canceled one.
        /// </summary>
        public readonly bool Canceled;

        public TouchNotification(TouchNotificationKind kind, Vector2 position, Vector2 clientArea, int id, bool primary, Vector2 contactArea, long touchDeviceID, Keys modifierKeys, object sender)
            : this(kind, position, clientArea, id, primary, contactArea, touchDeviceID, modifierKeys, sender, canceled: false)
        {
        }

        public TouchNotification(TouchNotificationKind kind, Vector2 position, Vector2 clientArea, int id, bool primary, Vector2 contactArea, long touchDeviceID, Keys modifierKeys, object sender, bool canceled)
            : base(position, clientArea, modifierKeys, sender)
        {
            Kind = kind;
            Id = id;
            Primary = primary;
            ContactArea = contactArea;
            TouchDeviceID = touchDeviceID;
            Canceled = canceled;
        }

        public bool IsTouchDown { get { return Kind == TouchNotificationKind.TouchDown; } }
        public bool IsTouchUp { get { return Kind == TouchNotificationKind.TouchUp; } }
        public bool IsTouchMove { get { return Kind == TouchNotificationKind.TouchMove; } }

        public override INotification WithSender(object sender)
            => new TouchNotification(Kind, Position, ClientArea, Id, Primary, ContactArea, TouchDeviceID, ModifierKeys, sender, Canceled);

        public override INotification Transform(INotificationSpaceTransformer transformer)
            => new TouchNotification(Kind,
                transformer.TransformPosition(Position),
                transformer.TransformSize(ClientArea),
                Id, Primary,
                transformer.TransformSize(ContactArea),
                TouchDeviceID,
                ModifierKeys,
                Sender,
                Canceled);
    }
}