using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.Stride.Utils
{
    public static class CameraUtils
    {
        public static Ray ScreenToWorldRay(CameraComponent cameraComponent, Vector2 position, float mouseSurfaceAspect = 0)
        {
            if (cameraComponent == null)
            {
                return new Ray();
            }

            // calculate projection matrix for current surface
            if (!cameraComponent.UseCustomAspectRatio && mouseSurfaceAspect > 0 && cameraComponent.ActuallyUsedAspectRatio != mouseSurfaceAspect)
                cameraComponent.Update(mouseSurfaceAspect);

            Matrix.Invert(ref cameraComponent.ViewProjectionMatrix, out var inverseViewProjection);

            Vector3 clipSpace;
            clipSpace.X = position.X * 2f - 1f;
            clipSpace.Y = 1f - position.Y * 2f;

            clipSpace.Z = 0f;
            Vector3.TransformCoordinate(ref clipSpace, ref inverseViewProjection, out var near);

            clipSpace.Z = 1f;
            Vector3.TransformCoordinate(ref clipSpace, ref inverseViewProjection, out var far);

            return new Ray(near, Vector3.Normalize(far - near));
        }
    }
}
