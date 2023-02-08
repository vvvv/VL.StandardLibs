using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Stride.Core.Mathematics;

namespace VL.Lib.IO.Notifications
{
    public enum GestureNotificationKind
    {
        GestureBegin = 1,
        GestureEnd = 2,
        GestureZoom = 3,
        GesturePan = 4,
        GestureRotate = 5,
        GestureTwoFingerTap = 6,
        GesturePressAndTap = 7
    }

    public class GestureNotification : NotificationWithPosition
    {
        public readonly GestureNotificationKind Kind;
        public readonly int Id;
        public readonly int SequenceId;
        public readonly long GestureDeviceID;
        public readonly int Flags;
        public readonly Int64 Arguments;
        public readonly int ExtraArguments;

        public GestureNotification(GestureNotificationKind kind, Vector2 position, Vector2 clientArea,
            int id, int sequenceId, long gestureDeviceID, int flags, Int64 ullArguments, int cbExtraArgs, Keys modifierKeys, object sender)
            : base(position, clientArea, modifierKeys, sender)
        {
            Kind = kind;
            Id = id;
            SequenceId = sequenceId;
            GestureDeviceID = gestureDeviceID;
            Flags = flags;
            Arguments = ullArguments;
            ExtraArguments = cbExtraArgs;
        }

        public bool IsGestureBegin { get { return Kind == GestureNotificationKind.GestureBegin; } }
        public bool IsGestureEnd { get { return Kind == GestureNotificationKind.GestureEnd; } }
        public bool IsGestureZoom { get { return Kind == GestureNotificationKind.GestureZoom; } }
        public bool IsGesturePan { get { return Kind == GestureNotificationKind.GesturePan; } }
        public bool IsGestureRotate { get { return Kind == GestureNotificationKind.GestureRotate; } }
        public bool IsGestureTwoFingerTap { get { return Kind == GestureNotificationKind.GestureTwoFingerTap; } }
        public bool IsGesturePressAndTap { get { return Kind == GestureNotificationKind.GesturePressAndTap; } }

        public override INotification WithSender(object sender)
            => new GestureNotification(Kind, Position, ClientArea, Id, SequenceId, GestureDeviceID, Flags, Arguments, ExtraArguments, ModifierKeys, sender);

        public override INotification Transform(INotificationSpaceTransformer transformer)
            => new GestureNotification(Kind, 
                transformer.TransformPosition(Position), 
                transformer.TransformSize(ClientArea), 
                Id, SequenceId, GestureDeviceID, Flags, Arguments, ExtraArguments, ModifierKeys, Sender);
    }
}