using System;
using Stride.Core.Mathematics;
using Stride.Rendering;
using VL.Lib.Mathematics;
using VL.UI.Core;

namespace VL.Stride.Rendering
{
    internal enum Sizing
    {
        Pixels,
        DIP,
        Normalized,
    }

    /// <summary>
    /// Objects are placed inside a space. Setting a space results in setting View and Projection matrices.
    /// </summary>
    public enum CommonSpace
    {
        /// <summary>
        /// The Space objects normally are placed within. 
        /// </summary>
        World,

        /// <summary>
        /// Place objects relative to the camera. (downstream View Matrix get ignored)
        /// </summary>
        View,

        /// <summary>
        /// Place objects relative to the projection. (downstream View and Projection Matrices get ignored)
        /// </summary>
        Projection,

        /// <summary>
        /// Height goes from 1 Top to -1 Bottom. The origin is located in the center. 
        /// </summary>
        Normalized,

        /// <summary>
        /// Works with device independant pixels. One unit equals 100 actual DIP. The origin is located in the center. Y-Axis points upwards.
        /// </summary>
        DIP,

        /// <summary>
        /// Works with device independant pixels. One unit equals 100 actual DIP. The origin is located at the top left. Y-Axis points upwards.
        /// </summary>
        DIPTopLeft,

        /// <summary>
        /// Works with pixels. One unit equals 100 actual pixels. The origin is located at the top left. Y-Axis points upwards.
        /// </summary>
        PixelTopLeft,
    };

    public static class ScreenSpaces
    {
        internal const float NearDefault = -100f;
        internal const float FarDefault  = 100f;

        internal static void GetWithinScreenSpaceTransformation(Rectangle viewportBounds, Sizing sizing,
            RectangleAnchor origin, float near, float far, out Matrix transformation)
        {
            transformation = Matrix.Identity;
            switch (sizing)
            {
                case Sizing.Normalized:
                    transformation.M11 = (float)viewportBounds.Height / viewportBounds.Width;
                    break;
                case Sizing.Pixels:
                    transformation.M11 = 200f / viewportBounds.Width;
                    transformation.M22 = 200f / viewportBounds.Height;
                    break;
                case Sizing.DIP:
                    transformation.M11 = DIPHelpers.DIPFactor() * 200f / viewportBounds.Width;
                    transformation.M22 = DIPHelpers.DIPFactor() * 200f / viewportBounds.Height;
                    break;
                default:
                    throw new NotImplementedException();
            }

            switch (origin.Horizontal())
            {
                case RectangleAnchorHorizontal.Left:
                    transformation.M41 = -1; 
                    break;
                case RectangleAnchorHorizontal.Center:
                    transformation.M41 = 0; 
                    break;
                case RectangleAnchorHorizontal.Right:
                    transformation.M41 = 1;
                    break;
                default:
                    break;
            }

            switch (origin.Vertical())
            {
                case RectangleAnchorVertical.Top:
                    transformation.M42 = 1; 
                    break;
                case RectangleAnchorVertical.Center:
                    transformation.M42 = 0;
                    break;
                case RectangleAnchorVertical.Bottom:
                    transformation.M42 = -1;
                    break;
                default:
                    break;
            }

            var depth = near - far;
            transformation.M33 = 1f / depth; // D3DXMatrixOrthoRH 
            transformation.M43 = near / depth;
        }

        internal static void GetWithinVirtualScreenSpaceTransformation(RectangleF userBounds,
            float near, float far, out Matrix transformation)
        {
            transformation = Matrix.Identity;
            transformation.M11 = 2f / userBounds.Width;
            transformation.M22 = 2f / userBounds.Height;

            transformation.M41 = -userBounds.Center.X * transformation.M11;
            transformation.M42 = -userBounds.Center.Y * transformation.M22;

            var depth = near - far;
            transformation.M33 = 1f / depth; // D3DXMatrixOrthoRH 
            transformation.M43 = near / depth;
        }

        internal static void GetWithinCommonSpaceTransformation(Rectangle viewportBounds, CommonSpace space, out Matrix transformation)
        {
            switch (space)
            {
                case CommonSpace.World:
                case CommonSpace.View:
                case CommonSpace.Projection:
                    throw new NotImplementedException();

                case CommonSpace.Normalized:
                    GetWithinScreenSpaceTransformation(viewportBounds, Sizing.Normalized, RectangleAnchor.Center, NearDefault, FarDefault, out transformation);
                    break;
                case CommonSpace.DIP:
                    GetWithinScreenSpaceTransformation(viewportBounds, Sizing.DIP, RectangleAnchor.Center, NearDefault, FarDefault, out transformation);
                    break;
                case CommonSpace.DIPTopLeft:
                    GetWithinScreenSpaceTransformation(viewportBounds, Sizing.DIP, RectangleAnchor.TopLeft, NearDefault, FarDefault, out transformation);
                    break;
                case CommonSpace.PixelTopLeft:
                    GetWithinScreenSpaceTransformation(viewportBounds, Sizing.Pixels, RectangleAnchor.TopLeft, NearDefault, FarDefault, out transformation);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        internal struct ViewAndProjectionRestore : IDisposable
        {
            private readonly RenderView renderView;
            private readonly Matrix previousView;
            private readonly Matrix previousProjection;
            private readonly Matrix previousViewProjection;

            public ViewAndProjectionRestore(RenderView renderView)
            {
                this.renderView = renderView;
                this.previousView = renderView.View;
                this.previousProjection = renderView.Projection;
                this.previousViewProjection = renderView.ViewProjection;
            }

            public void Dispose()
            {
                renderView.View = previousView;
                renderView.Projection = previousProjection;
                renderView.ViewProjection = previousViewProjection;
            }
        }

        internal static ViewAndProjectionRestore PushScreenSpace(this RenderView renderView, 
            ref Matrix view, ref Matrix projection, 
            bool ignoreExistingView, bool ignoreExistingProjection)
        {
            var result = new ViewAndProjectionRestore(renderView);

            if (ignoreExistingView)
                renderView.View = view;
            else
                Matrix.Multiply(ref view, ref renderView.View, out renderView.View);

            if (ignoreExistingProjection)
                renderView.Projection = projection;
            else
                Matrix.Multiply(ref projection, ref renderView.Projection, out renderView.Projection);
                
            Matrix.Multiply(ref renderView.View, ref renderView.Projection, out renderView.ViewProjection);

            return result;
        }


        public static RectangleAnchor FlipAnchorForStride(RectangleAnchor anchor)
        {
            if (((int)anchor & (int)RectangleAnchorVertical.Top) > 0)
                anchor = (RectangleAnchor)((int)anchor - (int)RectangleAnchorVertical.Top + RectangleAnchorVertical.Bottom);
            else
            if (((int)anchor & (int)RectangleAnchorVertical.Bottom) > 0)
                anchor = (RectangleAnchor)((int)anchor - (int)RectangleAnchorVertical.Bottom + RectangleAnchorVertical.Top);
            return anchor;
        }
    }


    public abstract class AbstractSpaceNode : RendererBase
    {
        public bool IgnoreExistingView { get; set; } = true;
        public bool IgnoreExistingProjection { get; set; } = true;
        protected Matrix Identity = Matrix.Identity;
    }


    public class WithinCommonSpace : AbstractSpaceNode
    {
        public CommonSpace CommonScreenSpace { get; set; } = CommonSpace.DIPTopLeft;

        protected override void DrawInternal(RenderDrawContext context)
        {
            switch (CommonScreenSpace)
            {
                case CommonSpace.World:
                    DrawInput(context);
                    break;
                case CommonSpace.View:
                case CommonSpace.Projection:
                    Matrix proj;
                    if (CommonScreenSpace == CommonSpace.View)
                        proj = context.RenderContext.RenderView.Projection;
                    else
                        proj = Identity;

                    using (context.RenderContext.RenderView.PushScreenSpace(
                        ref Identity, ref proj,
                        true, true))
                    {
                        DrawInput(context);
                    }
                    break;
                case CommonSpace.Normalized:
                case CommonSpace.DIP:
                case CommonSpace.DIPTopLeft:
                case CommonSpace.PixelTopLeft:
                    ScreenSpaces.GetWithinCommonSpaceTransformation(context.RenderContext.ViewportState.Viewport0.Bounds,
                        CommonScreenSpace, out var m);
                    using (context.RenderContext.RenderView.PushScreenSpace(
                        ref Identity, ref m,
                        true, true))
                    {
                        DrawInput(context);
                    }
                    break;
                default:
                    break;
            }
        }
    }

    public enum ScreenSpaceUnits { Pixels, DIP }

    public class WithinPhysicalScreenSpace : AbstractSpaceNode
    {
        public ScreenSpaceUnits Units { get; set; } = ScreenSpaceUnits.DIP;
        public float Scale { get; set; } = 1f;
        public Vector2 Offset { get; set; }
        public RectangleAnchor Anchor { get; set; } = RectangleAnchor.Center;

        protected override void DrawInternal(RenderDrawContext context)
        {
            var position = -Offset;
            var scaling = Scale;
            if (Units == ScreenSpaceUnits.DIP)
                scaling *= DIPHelpers.DIPFactor();
            var viewPort = context.RenderContext.ViewportState.Viewport0;
            var viewPortSize = new Vector2(viewPort.Size.X * 0.01f, viewPort.Size.Y * 0.01f);
            var size = viewPortSize / scaling;
            var anchor = ScreenSpaces.FlipAnchorForStride(Anchor);

            RectangleNodes.Join(ref position, ref size, anchor, out var bounds);

            ScreenSpaces.GetWithinVirtualScreenSpaceTransformation(
                bounds, ScreenSpaces.NearDefault, ScreenSpaces.FarDefault, out var m);

            using (context.RenderContext.RenderView.PushScreenSpace(
                ref Identity, ref m,
                IgnoreExistingView, IgnoreExistingProjection))
            {
                DrawInput(context);
            }
        }
    }

    public class WithinVirtualScreenSpace : AbstractSpaceNode
    {
        public RectangleF Bounds { get; set; } = new RectangleF(-0.5f, -0.5f, 1, 1);
        public SizeMode AspectRatioCorrectionMode { get; set; } = SizeMode.FitOut;
        public RectangleAnchor Anchor { get; set; } = RectangleAnchor.Center;

        protected override void DrawInternal(RenderDrawContext context)
        {
            var userBounds = Bounds;
            var viewPort = context.RenderContext.ViewportState.Viewport0;
            var viewPortSize = new Vector2(viewPort.Size.X * 0.01f, viewPort.Size.Y * 0.01f);

            AspectRatioUtils.FixAspectRatio(
                ref userBounds,
                ref viewPortSize,
                AspectRatioCorrectionMode,
                ScreenSpaces.FlipAnchorForStride(Anchor),
                out var actualBounds
                );

            ScreenSpaces.GetWithinVirtualScreenSpaceTransformation(
                actualBounds, ScreenSpaces.NearDefault, ScreenSpaces.FarDefault, out var m);

            using (context.RenderContext.RenderView.PushScreenSpace(
                ref Identity, ref m,
                IgnoreExistingView, IgnoreExistingProjection))
            {
                DrawInput(context);
            }
        }
    }
}
