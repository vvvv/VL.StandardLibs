#nullable enable
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using VL.Lib.Mathematics;
using VL.Stride.Rendering;

[assembly: ImportType(typeof(StereoscopicSettings))]

namespace VL.Stride.Rendering;

/// <summary>
/// Viewport settings for stereoscopic rendering
/// </summary>
[ProcessNode]
public sealed class StereoscopicSettings
{
    private float eyeSeparation;
    private float viewerDistance;
    private readonly ViewportSettings viewportSettings;

    public StereoscopicSettings()
    {
        viewportSettings = new ViewportSettings()
        {
            ViewportRenderInfo = new StereoscopicRenderInfo(this),
            Views = new List<ViewportView>()
            {
                new ViewportView()
                {
                    View = new RenderView()
                },
                new ViewportView()
                {
                    View = new RenderView()
                }
            }
        };
    }

    /// <summary>
    /// Updates the stereoscopic settings with the given eye separation and viewer distance.
    /// </summary>
    /// <param name="eyeSeparation">The distance between the eyes for stereoscopic rendering.</param>
    /// <param name="viewerDistance">The distance from the viewer to the screen for stereoscopic rendering.</param>
    /// <returns>The updated viewport settings.</returns>
    public ViewportSettings Update(float eyeSeparation = 0.065f, float viewerDistance = 1.0f)
    {
        this.eyeSeparation = eyeSeparation;
        this.viewerDistance = viewerDistance;
        UpdateViews(viewportSettings.ViewportRenderInfo?.CameraComponent);
        return viewportSettings;
    }

    private void UpdateViews(CameraComponent cameraComponent)
    {
        if (cameraComponent is null)
            return;

        var leftViewport = viewportSettings.Views[0];
        var rightViewport = viewportSettings.Views[1];

        var renderTargetSize = viewportSettings.ViewportRenderInfo?.RenderTargetSize ?? default;
        var fullWidth = renderTargetSize.X;
        var fullHeight = renderTargetSize.Y;
        var halfWidth = fullWidth * 0.5f;

        if (halfWidth > 0f && fullHeight > 0f)
        {
            leftViewport.Viewport = new ViewportF(0f, 0f, halfWidth, fullHeight);
            rightViewport.Viewport = new ViewportF(halfWidth, 0f, halfWidth, fullHeight);
        }

        var aspectRatio = cameraComponent.UseCustomAspectRatio
            ? cameraComponent.AspectRatio
            : (halfWidth > 0f && fullHeight > 0f ? halfWidth / fullHeight : cameraComponent.ActuallyUsedAspectRatio);

        cameraComponent.Update(aspectRatio);

        var baseView = cameraComponent.ViewMatrix;
        var baseProjection = cameraComponent.ProjectionMatrix;
        var nearClip = cameraComponent.NearClipPlane;
        var farClip = cameraComponent.FarClipPlane;
        var halfEyeSeparation = eyeSeparation * 0.5f;
        var projectionShiftScale = Math.Abs(viewerDistance) > float.Epsilon ? nearClip / viewerDistance : 0f;

        static void UpdateEyeView(ref RenderView renderView, in Matrix baseView, in Matrix baseProjection, float nearClip, float farClip, float eyeOffset, float projectionShiftScale)
        {
            var view = baseView;
            // Move eye along local X in view-space (parallel-axis stereo).
            view.M41 -= eyeOffset;

            var projection = baseProjection;
            if (projection.M11 != 0f && projection.M22 != 0f && nearClip > 0f)
            {
                var left = nearClip * (projection.M31 - 1.0f) / projection.M11;
                var right = nearClip * (projection.M31 + 1.0f) / projection.M11;
                var bottom = nearClip * (projection.M32 - 1.0f) / projection.M22;
                var top = nearClip * (projection.M32 + 1.0f) / projection.M22;

                var projectionShift = -eyeOffset * projectionShiftScale;
                Matrix.PerspectiveOffCenterRH(left + projectionShift, right + projectionShift, bottom, top, nearClip, farClip, out projection);
            }

            renderView.View = view;
            renderView.Projection = projection;
            renderView.NearClipPlane = nearClip;
            renderView.FarClipPlane = farClip;
            Matrix.Multiply(ref renderView.View, ref renderView.Projection, out renderView.ViewProjection);
            renderView.Frustum = new BoundingFrustum(ref renderView.ViewProjection);
            renderView.CullingMode = CameraCullingMode.Frustum;
        }

        UpdateEyeView(ref leftViewport.View, baseView, baseProjection, nearClip, farClip, -halfEyeSeparation, projectionShiftScale);
        UpdateEyeView(ref rightViewport.View, baseView, baseProjection, nearClip, farClip, halfEyeSeparation, projectionShiftScale);
    }

    private sealed class StereoscopicRenderInfo : ViewportRenderInfo
    {
        private readonly StereoscopicSettings parent;

        public StereoscopicRenderInfo(StereoscopicSettings parent)
        {
            this.parent = parent;
        }

        public override CameraComponent CameraComponent 
        { 
            get => base.CameraComponent;
            set
            {
                base.CameraComponent = value;
                parent.UpdateViews(CameraComponent);
            }
        }

        public override Vector2 RenderTargetSize 
        { 
            get => base.RenderTargetSize; 
            set
            {
                base.RenderTargetSize = value;
                parent.UpdateViews(CameraComponent);
            }
        }
    }
}
