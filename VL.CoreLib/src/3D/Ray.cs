using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;

namespace VL.Lib.Mathematics
{
    public static class RayNodes
    {
        /// <summary>
        /// Calculates a world space <see cref="Ray"/> from 2d screen coordinates.
        /// </summary>
        /// <param name="x">X coordinate on 2d screen.</param>
        /// <param name="y">Y coordinate on 2d screen.</param>
        /// <param name="viewport"><see cref="ViewportF"/>.</param>
        /// <param name="worldViewProjection">Transformation <see cref="Matrix"/>.</param>
        /// <returns>Resulting <see cref="Ray"/>.</returns>
        public static Ray GetPickRay(int x, int y, ViewportF viewport, Matrix worldViewProjection)
        {
            var nearPoint = new Vector3(x, y, 0);
            var farPoint = new Vector3(x, y, 1);

            nearPoint = Vector3.Unproject(nearPoint, viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinDepth,
                                        viewport.MaxDepth, worldViewProjection);
            farPoint = Vector3.Unproject(farPoint, viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinDepth,
                                        viewport.MaxDepth, worldViewProjection);

            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();

            return new Ray(nearPoint, direction);
        }
    }
}
