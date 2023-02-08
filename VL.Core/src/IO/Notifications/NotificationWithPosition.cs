using Stride.Core.Mathematics;
using System;

namespace VL.Lib.IO.Notifications
{
    public interface INotificationWithPosition : INotification
    {
        Vector2 Position { get; }
        Vector2 ClientArea { get; }
    }

    public interface INotificationWithSpacePositions
    {
        Vector2 PositionInProjectionSpace { get; }
        Vector2 PositionInNormalizedProjectionSpace { get; }
        Vector2 PositionInWorldSpace { get; }
    }

    public abstract class NotificationWithPosition : NotificationBase, INotificationWithPosition, INotificationWithSpacePositions
    {
        public readonly Vector2 Position;
        public readonly Vector2 ClientArea;

        public NotificationWithPosition(Vector2 position, Vector2 clientArea, Keys modifierKeys, object sender)
            : base(sender, modifierKeys)
        {
            Position = position;
            ClientArea = clientArea;

            spacePositions = new Lazy<SpacePositions>(() =>
            {
                var ps = new SpacePositions();
                SpaceHelpers.MapFromPixels(this, out ps.InNormalizedProjectionSpace, out ps.InProjectionSpace, out ps.InWorldSpace);
                return ps;
            });
        }
        Vector2 INotificationWithPosition.ClientArea => ClientArea;
        Vector2 INotificationWithPosition.Position => Position;

        Lazy<SpacePositions> spacePositions; 
        public Vector2 PositionInProjectionSpace => spacePositions.Value.InProjectionSpace;
        public Vector2 PositionInNormalizedProjectionSpace => spacePositions.Value.InNormalizedProjectionSpace;
        public Vector2 PositionInWorldSpace => spacePositions.Value.InWorldSpace;
    }

    internal struct SpacePositions
    {
        public Vector2 InProjectionSpace;
        public Vector2 InNormalizedProjectionSpace;
        public Vector2 InWorldSpace;
    }

    public class NotificationWithClientArea : NotificationBase
    {
        public readonly Vector2 ClientArea;
        public NotificationWithClientArea(Vector2 clientArea, Keys modifierKeys, object sender)
            : base(sender, modifierKeys)
        {
            ClientArea = clientArea;
        }

        public override INotification WithSender(object sender)
            => new NotificationWithClientArea(ClientArea, ModifierKeys, sender);

        public override INotification Transform(INotificationSpaceTransformer transformer)
            => new NotificationWithClientArea(transformer.TransformSize(ClientArea), ModifierKeys, Sender);

    }

}
