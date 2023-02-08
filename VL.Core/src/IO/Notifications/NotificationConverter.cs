using System;
using System.Runtime.Versioning;
using Stride.Core.Mathematics;
using VL.UI.Core;

namespace VL.Lib.IO.Notifications
{
    public interface INotificationSpaceTransformer
    {
        Vector2 TransformPosition(Vector2 point);
        Vector2 TransformSize(Vector2 size);
    }

    public class DummyNotificationSpaceTransformer : INotificationSpaceTransformer
    {
        public static INotificationSpaceTransformer Instance = new DummyNotificationSpaceTransformer();
        public Vector2 TransformSize(Vector2 size) => size;
        public Vector2 TransformPosition(Vector2 point) => point;
    }

    [SupportedOSPlatform("windows")]
    public class DIPNotificationSpaceTransformer : INotificationSpaceTransformer
    {
        public static INotificationSpaceTransformer Instance = new DIPNotificationSpaceTransformer();

        float Scaling;

        public DIPNotificationSpaceTransformer()
        {
            Scaling = 1 / DIPHelpers.DIPFactor();
        }

        public Vector2 TransformSize(Vector2 size)
        {
            return new Vector2(size.X * Scaling, size.Y * Scaling);
        }

        public Vector2 TransformPosition(Vector2 point)
        {
            return new Vector2(point.X * Scaling, point.Y * Scaling);
        }
    }

    public class NotificationSpaceTransformer : INotificationSpaceTransformer
    {
        readonly Vector2 Offset;
        readonly Vector2 Scaling;

        public NotificationSpaceTransformer(Vector2 offset, Vector2 scaling)
        {
            Offset = offset;
            Scaling = scaling;
        }

        public Vector2 TransformPosition(Vector2 point)
        {
            // discuss: do we really want to specify the offset in the original coordinate system?
            return new Vector2((point.X + Offset.X) * Scaling.X, (point.Y + Offset.Y) * Scaling.Y);
        }

        public Vector2 TransformSize(Vector2 size)
        {
            return new Vector2(size.X * Scaling.X, size.Y * Scaling.Y);
        }
    }

    // not yet used 
    public class MatrixNotificationSpaceTransformer : INotificationSpaceTransformer
    {
        Matrix M;

        public MatrixNotificationSpaceTransformer(Matrix matrix)
        {
            M = matrix;
        }

        public Vector2 TransformPosition(Vector2 point) => Vector2.TransformCoordinate(point, M);
        public Vector2 TransformSize(Vector2 size) => Vector2.TransformNormal(size, M);
    }

    public static class NotificationConverter
    {
        public static object ConvertNotification(object notification, INotificationSpaceTransformer transformer)
            => (notification as INotification)?.Transform(transformer) ?? notification;
    }
}
