using Stride.Core.Mathematics;
using SkiaSharp;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;
using VL.Lib.Mathematics;

namespace VL.Skia
{
    class IntermediateTransformNotificationSender : IWorldSpace2d, IProjectionSpace
    {
        SKMatrix InvTransformation;
        INotification OriginalNotification;
        public IntermediateTransformNotificationSender(SKMatrix invTransformation, INotification originalNotification)
        {
            InvTransformation = invTransformation;
            OriginalNotification = originalNotification;
        }

        public Vector2 MapFromPixels(INotificationWithPosition notification)
        {
            SKPoint p;
            var n = OriginalNotification as INotificationWithPosition;
            p = new SKPoint(n.Position.X, n.Position.Y);
            var relativePosition = InvTransformation.MapPoint(p);
            return new Vector2(relativePosition.X, relativePosition.Y);
        }

        public void MapFromPixels(INotificationWithPosition notification, out Vector2 inNormalizedProjection, out Vector2 inProjection)
        {
            var n = OriginalNotification as INotificationWithSpacePositions;
            if (n != null)
            {
                inNormalizedProjection = n.PositionInNormalizedProjectionSpace;
                inProjection = n.PositionInProjectionSpace;
            }
            else
            {
                inNormalizedProjection = Vector2.Zero;
                inProjection = Vector2.Zero;
            }
        }
    }

    public class TransformUpstream : LinkedLayerBase
    {
        SKMatrix M;

        public void Update(ILayer input, SKMatrix transformation, out ILayer output)
        {
            Input = input;
            M = transformation;
            output = this;
        }

        public override void Render(CallerInfo caller)
        {
            var us = caller.PushTransformation(M);
            us.Canvas.SetMatrix(us.Transformation);
            try
            {
                Input?.Render(us);
            }
            finally
            {
                caller.Canvas.SetMatrix(caller.Transformation);
            }
        }

        public override bool Notify(INotification notification, CallerInfo caller)
        {
            var us = caller.PushTransformation(M);
            SKMatrix invTransformation;
            us.Transformation.TryInvert(out invTransformation);
            var newNotification = notification.WithSender(new IntermediateTransformNotificationSender(invTransformation, notification));
            return base.Notify(newNotification, us);
        }

        public override RectangleF? Bounds 
        {
            get
            {
                if (Input?.Bounds != null)
                    return M.MapRect(Input.Bounds.Value.ToSKRect_()).ToRectangleF_();
                else
                    return null;
            } 
        }
    }

    public class SetSpaceUpstreamBase : LinkedLayerBase
    {
        public SKMatrix Transformation { get; set; }

        public override void Render(CallerInfo caller)
        {
            var us = caller.WithTransformation(Transformation);
            us.Canvas.SetMatrix(us.Transformation);
            try
            {
                Input?.Render(us);
            }
            finally
            {
                caller.Canvas.SetMatrix(caller.Transformation);
            }
        }

        public override bool Notify(INotification notification, CallerInfo caller)
        {
            var us = caller.WithTransformation(Transformation);
            SKMatrix invTransformation;
            us.Transformation.TryInvert(out invTransformation);
            var newNotification = notification.WithSender(new IntermediateTransformNotificationSender(invTransformation, notification));
            return base.Notify(newNotification, us);
        }
    }

    public class SetSpaceUpstream : SetSpaceUpstreamBase
    {
        Sizing Sizing;
        float Width;
        float Height;
        RectangleAnchor Origin;

        public void Update(ILayer input, out ILayer output, Sizing sizing = Sizing.ManualSize,
            float width = 0, float height = 2, RectangleAnchor origin = RectangleAnchor.Center)
        {
            Input = input;
            Sizing = sizing;
            Width = width;
            Height = height;
            Origin = origin;
            output = this;
        }

        public override void Render(CallerInfo caller)
        {
            Transformation = CallerInfoExtensions.GetWithinSpaceTransformation(caller.ViewportBounds, Sizing, Width, Height, Origin);
            base.Render(caller);
        }

        public override bool Notify(INotification notification, CallerInfo caller)
        {
            Transformation = CallerInfoExtensions.GetWithinSpaceTransformation(caller.ViewportBounds, Sizing, Width, Height, Origin);
            return base.Notify(notification, caller);
        }
    }

    public class SetSpaceUpstream2 : SetSpaceUpstreamBase
    {
        CommonSpace Space;
        
        public void UpdateTransformation(SKRect viewportBounds, CommonSpace space)
        {
            Transformation = CallerInfoExtensions.GetWithinCommonSpaceTransformation(viewportBounds, space);
        }

        public void Update(ILayer input, out ILayer output, CommonSpace space = CommonSpace.Normalized)
        {
            Input = input;
            Space = space; 
            output = this;
        }

        public override void Render(CallerInfo caller)
        {
            UpdateTransformation(caller.ViewportBounds, Space);
            base.Render(caller);
        }

        public override bool Notify(INotification notification, CallerInfo caller)
        {
            UpdateTransformation(caller.ViewportBounds, Space);
            return base.Notify(notification, caller);
        }
    }


    public class InViewportUpstream : LinkedLayerBase
    {
        SKRect bounds;
        CommonSpace Space;

        public void Update(ILayer input, SKRect bounds, CommonSpace space, out ILayer output)
        {
            Input = input;
            this.bounds = bounds;
            Space = space;
            output = this;
        }

        public override void Render(CallerInfo caller)
        {
            // get bounds into window pixel space
            var boundsInPixels = caller.Transformation.MapRect(bounds);

            // a new callerInfo view the new viewport and transform settings
            var us = caller.InViewport(boundsInPixels, c => CallerInfoExtensions.GetWithinCommonSpaceTransformation(c.ViewportBounds, Space));
            us.Canvas.SetMatrix(us.Transformation);
            try
            {
                Input?.Render(us);
            }
            finally
            {
                caller.Canvas.SetMatrix(caller.Transformation);
            }
        }

        public override RectangleF? Bounds => bounds.ToRectangleF_();

        class ViewportNotificationSender : IWorldSpace2d, IProjectionSpace
        {
            SKMatrix InvTransformation;
            INotification OriginalNotification;
            SKRect BoundsInPixels;

            public ViewportNotificationSender(SKMatrix invTransformation, INotification originalNotification, SKRect boundsInPixels)
            {
                InvTransformation = invTransformation;
                OriginalNotification = originalNotification;
                BoundsInPixels = boundsInPixels;
            }

            public Vector2 MapFromPixels(INotificationWithPosition notification)
            {
                SKPoint p;
                var n = OriginalNotification as INotificationWithPosition;
                p = new SKPoint(n.Position.X, n.Position.Y);
                var relativePosition = InvTransformation.MapPoint(p);
                return new Vector2(relativePosition.X, relativePosition.Y);
            }

            public void MapFromPixels(INotificationWithPosition notification, out Vector2 inNormalizedProjection, out Vector2 inProjection)
            {
                var pos = new Vector2(notification.Position.X - BoundsInPixels.Location.X, notification.Position.Y - BoundsInPixels.Location.Y);
                SpaceHelpers.DoMapFromPixels(pos, new Vector2(BoundsInPixels.Width, BoundsInPixels.Height), out inNormalizedProjection, out inProjection);
            }
        }

        public override bool Notify(INotification notification, CallerInfo caller)
        {
            // get bounds into window pixel space
            var boundsInPixels = caller.Transformation.MapRect(bounds);

            // a new callerInfo view the new viewport and transform settings
            caller = caller.InViewport(boundsInPixels, c => CallerInfoExtensions.GetWithinCommonSpaceTransformation(c.ViewportBounds, Space));

            SKMatrix invTransformation;
            caller.Transformation.TryInvert(out invTransformation);
            var newNotification = notification.WithSender(new ViewportNotificationSender(invTransformation, notification, boundsInPixels));
            return base.Notify(newNotification, caller);
        }
    }
}
