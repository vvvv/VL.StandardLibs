using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;

namespace VL.Lib._3D
{
    public static class BoundingFrustumNodes
    {
        /// <summary>
        /// Determines whether the specified <see cref="BoundingFrustum"/> is equal to this instance.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="other">The <see cref="BoundingFrustum"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="BoundingFrustum"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // MethodImplOptions.AggressiveInlining
        public static bool Equals(ref BoundingFrustum input, ref BoundingFrustum other)
        {
            //TODO: make proper equals implementation in xenko type
            return input.Equals(other);
        }

        /// <summary>
        /// Determines whether the specified <see cref="BoundingFrustum"/> is not equal to this instance.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="other">The <see cref="BoundingFrustum"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="BoundingFrustum"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // MethodImplOptions.AggressiveInlining
        public static bool NotEquals(ref BoundingFrustum input, ref BoundingFrustum other)
        {
            //TODO: make proper equals implementation in xenko type
            return !input.Equals(other);
        }

        /// <summary>
        /// Indicate whether the current BoundingFrustrum is Orthographic.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the current BoundingFrustrum is Orthographic; otherwise, <c>false</c>.
        /// </value>
        public static bool IsOrthographic(ref BoundingFrustum input)
        {
            return (input.LeftPlane.Normal == -input.RightPlane.Normal) && (input.TopPlane.Normal == -input.BottomPlane.Normal);
        }

        /// <summary>
        /// Returns one of the 6 planes related to this frustum.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="index">Plane index where 0 for Left, 1 for Right, 2 for Top, 3 for Bottom, 4 for Near, 5 for Far</param>
        /// <returns></returns>
        public static Plane GetPlane(ref BoundingFrustum input, int index)
        {
            switch (index)
            {
                case 0: return input.LeftPlane;
                case 1: return input.RightPlane;
                case 2: return input.TopPlane;
                case 3: return input.BottomPlane;
                case 4: return input.NearPlane;
                case 5: return input.FarPlane;
                default:
                    return new Plane();
            }
        }

        private static Vector3 Get3PlanesInterPoint(ref Plane p1, ref Plane p2, ref Plane p3)
        {
            //P = -d1 * N2xN3 / N1.N2xN3 - d2 * N3xN1 / N2.N3xN1 - d3 * N1xN2 / N3.N1xN2 
            Vector3 v =
                -p1.D * Vector3.Cross(p2.Normal, p3.Normal) / Vector3.Dot(p1.Normal, Vector3.Cross(p2.Normal, p3.Normal))
                - p2.D * Vector3.Cross(p3.Normal, p1.Normal) / Vector3.Dot(p2.Normal, Vector3.Cross(p3.Normal, p1.Normal))
                - p3.D * Vector3.Cross(p1.Normal, p2.Normal) / Vector3.Dot(p3.Normal, Vector3.Cross(p1.Normal, p2.Normal));

            return v;
        }

        /// <summary>
        /// Returns the 8 corners of the frustum, element0 is Near1 (near right down corner)
        /// , element1 is Near2 (near right top corner)
        /// , element2 is Near3 (near Left top corner)
        /// , element3 is Near4 (near Left down corner)
        /// , element4 is Far1 (far right down corner)
        /// , element5 is Far2 (far right top corner)
        /// , element6 is Far3 (far left top corner)
        /// , element7 is Far4 (far left down corner)
        /// </summary>
        /// <returns>The 8 corners of the frustum</returns>
        public static void GetCorners(ref BoundingFrustum input, Vector3[] corners)
        {
            corners[0] = Get3PlanesInterPoint(ref input.NearPlane, ref input.BottomPlane, ref input.RightPlane);    //Near1
            corners[1] = Get3PlanesInterPoint(ref input.NearPlane, ref input.TopPlane, ref input.RightPlane);       //Near2
            corners[2] = Get3PlanesInterPoint(ref input.NearPlane, ref input.TopPlane, ref input.LeftPlane);        //Near3
            corners[3] = Get3PlanesInterPoint(ref input.NearPlane, ref input.BottomPlane, ref input.LeftPlane);     //Near3
            corners[4] = Get3PlanesInterPoint(ref input.FarPlane, ref input.BottomPlane, ref input.RightPlane);    //Far1
            corners[5] = Get3PlanesInterPoint(ref input.FarPlane, ref input.TopPlane, ref input.RightPlane);       //Far2
            corners[6] = Get3PlanesInterPoint(ref input.FarPlane, ref input.TopPlane, ref input.LeftPlane);        //Far3
            corners[7] = Get3PlanesInterPoint(ref input.FarPlane, ref input.BottomPlane, ref input.LeftPlane);     //Far3
        }

        /// <summary>
        /// Returns the 8 corners of the frustum, element0 is Near1 (near right down corner)
        /// , element1 is Near2 (near right top corner)
        /// , element2 is Near3 (near Left top corner)
        /// , element3 is Near4 (near Left down corner)
        /// , element4 is Far1 (far right down corner)
        /// , element5 is Far2 (far right top corner)
        /// , element6 is Far3 (far left top corner)
        /// , element7 is Far4 (far left down corner)
        /// </summary>
        /// <returns>The 8 corners of the frustum</returns>
        public static Vector3[] GetCorners(ref BoundingFrustum input)
        {
            var corners = new Vector3[8];
            GetCorners(ref input, corners);
            return corners;
        }

        /// <summary>
        /// Checks whether a point lay inside, intersects or lay outside the frustum.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="point">The point.</param>
        /// <returns>Type of the containment</returns>
        public static ContainmentType Contains(ref BoundingFrustum input, ref Vector3 point)
        {
            var result = PlaneIntersectionType.Front;
            var planeResult = PlaneIntersectionType.Front;
            for (int i = 0; i < 6; i++)
            {
                switch (i)
                {
                    case 0: planeResult = input.NearPlane.Intersects(ref point); break;
                    case 1: planeResult = input.FarPlane.Intersects(ref point); break;
                    case 2: planeResult = input.LeftPlane.Intersects(ref point); break;
                    case 3: planeResult = input.RightPlane.Intersects(ref point); break;
                    case 4: planeResult = input.TopPlane.Intersects(ref point); break;
                    case 5: planeResult = input.BottomPlane.Intersects(ref point); break;
                }
                switch (planeResult)
                {
                    case PlaneIntersectionType.Back:
                        return ContainmentType.Disjoint;
                    case PlaneIntersectionType.Intersecting:
                        result = PlaneIntersectionType.Intersecting;
                        break;
                }
            }
            switch (result)
            {
                case PlaneIntersectionType.Intersecting: return ContainmentType.Intersects;
                default: return ContainmentType.Contains;
            }
        }

        /// <summary>
        /// Determines the intersection relationship between the frustum and a bounding box.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="box">The box.</param>
        /// <returns>Type of the containment</returns>
        public static ContainmentType Contains(ref BoundingFrustum input, ref BoundingBox box)
        {
            Vector3 p, n;
            Plane plane;
            var result = ContainmentType.Contains;
            for (int i = 0; i < 6; i++)
            {
                plane = GetPlane(ref input, i);
                GetBoxToPlanePVertexNVertex(ref box, ref plane.Normal, out p, out n);
                if (CollisionHelper.PlaneIntersectsPoint(ref plane, ref p) == PlaneIntersectionType.Back)
                    return ContainmentType.Disjoint;

                if (CollisionHelper.PlaneIntersectsPoint(ref plane, ref n) == PlaneIntersectionType.Back)
                    result = ContainmentType.Intersects;
            }
            return result;
        }

        private static void GetBoxToPlanePVertexNVertex(ref BoundingBox box, ref Vector3 planeNormal, out Vector3 p, out Vector3 n)
        {
            p = box.Minimum;
            if (planeNormal.X >= 0)
                p.X = box.Maximum.X;
            if (planeNormal.Y >= 0)
                p.Y = box.Maximum.Y;
            if (planeNormal.Z >= 0)
                p.Z = box.Maximum.Z;

            n = box.Maximum;
            if (planeNormal.X >= 0)
                n.X = box.Minimum.X;
            if (planeNormal.Y >= 0)
                n.Y = box.Minimum.Y;
            if (planeNormal.Z >= 0)
                n.Z = box.Minimum.Z;
        }

        /// <summary>
        /// Determines the intersection relationship between the frustum and a bounding sphere.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="sphere">The sphere.</param>
        /// <returns>Type of the containment</returns>
        public static ContainmentType Contains(ref BoundingFrustum input, ref BoundingSphere sphere)
        {
            var result = PlaneIntersectionType.Front;
            var planeResult = PlaneIntersectionType.Front;
            for (int i = 0; i < 6; i++)
            {
                switch (i)
                {
                    case 0: planeResult = input.NearPlane.Intersects(ref sphere); break;
                    case 1: planeResult = input.FarPlane.Intersects(ref sphere); break;
                    case 2: planeResult = input.LeftPlane.Intersects(ref sphere); break;
                    case 3: planeResult = input.RightPlane.Intersects(ref sphere); break;
                    case 4: planeResult = input.TopPlane.Intersects(ref sphere); break;
                    case 5: planeResult = input.BottomPlane.Intersects(ref sphere); break;
                }
                switch (planeResult)
                {
                    case PlaneIntersectionType.Back:
                        return ContainmentType.Disjoint;
                    case PlaneIntersectionType.Intersecting:
                        result = PlaneIntersectionType.Intersecting;
                        break;
                }
            }
            switch (result)
            {
                case PlaneIntersectionType.Intersecting: return ContainmentType.Intersects;
                default: return ContainmentType.Contains;
            }
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum intersects a BoundingSphere.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="sphere">The sphere.</param>
        /// <returns>Type of the containment</returns>
        public static bool Intersects(ref BoundingFrustum input, ref BoundingSphere sphere)
        {
            return Contains(ref input, ref sphere) != ContainmentType.Disjoint;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum intersects a BoundingBox.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="box">The box.</param>
        /// <returns><c>true</c> if the current BoundingFrustum intersects a BoundingSphere.</returns>
        public static bool Intersects(ref BoundingFrustum input, ref BoundingBox box)
        {
            return Contains(ref input, ref box) != ContainmentType.Disjoint;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum intersects the specified Plane.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="plane">The plane.</param>
        /// <returns>Plane intersection type.</returns>
        public static PlaneIntersectionType Intersects(ref BoundingFrustum input, ref Plane plane)
        {
            return PlaneIntersectsPoints(ref plane, GetCorners(ref input));
        }

        private static PlaneIntersectionType PlaneIntersectsPoints(ref Plane plane, Vector3[] points)
        {
            var result = CollisionHelper.PlaneIntersectsPoint(ref plane, ref points[0]);
            for (int i = 1; i < points.Length; i++)
                if (CollisionHelper.PlaneIntersectsPoint(ref plane, ref points[i]) != result)
                    return PlaneIntersectionType.Intersecting;
            return result;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum intersects the specified Ray.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="ray">The Ray to check for intersection with.</param>
        /// <param name="inDistance">The distance at which the ray enters the frustum if there is an intersection and the ray starts outside the frustum.</param>
        /// <param name="outDistance">The distance at which the ray exits the frustum if there is an intersection.</param>
        /// <returns><c>true</c> if the current BoundingFrustum intersects the specified Ray.</returns>
        public static bool Intersects(ref BoundingFrustum input, ref Ray ray, out float? inDistance, out float? outDistance)
        {
            if (Contains(ref input, ref ray.Position) != ContainmentType.Disjoint)
            {
                float nearstPlaneDistance = float.MaxValue;
                for (int i = 0; i < 6; i++)
                {
                    var plane = GetPlane(ref input, i);
                    float distance;
                    if (CollisionHelper.RayIntersectsPlane(ref ray, ref plane, out distance) && distance < nearstPlaneDistance)
                    {
                        nearstPlaneDistance = distance;
                    }
                }

                inDistance = nearstPlaneDistance;
                outDistance = null;
                return true;
            }
            else
            {
                //We will find the two points at which the ray enters and exists the frustum
                //These two points make a line which center inside the frustum if the ray intersects it
                //Or outside the frustum if the ray intersects frustum planes outside it.
                float minDist = float.MaxValue;
                float maxDist = float.MinValue;
                for (int i = 0; i < 6; i++)
                {
                    var plane = GetPlane(ref input, i);
                    float distance;
                    if (CollisionHelper.RayIntersectsPlane(ref ray, ref plane, out distance))
                    {
                        minDist = Math.Min(minDist, distance);
                        maxDist = Math.Max(maxDist, distance);
                    }
                }

                Vector3 minPoint = ray.Position + ray.Direction * minDist;
                Vector3 maxPoint = ray.Position + ray.Direction * maxDist;
                Vector3 center = (minPoint + maxPoint) / 2f;
                if (Contains(ref input, ref center) != ContainmentType.Disjoint)
                {
                    inDistance = minDist;
                    outDistance = maxDist;
                    return true;
                }
                else
                {
                    inDistance = null;
                    outDistance = null;
                    return false;
                }
            }
        }

        /// <summary>
        /// Creates a new frustum relaying on perspective camera parameters
        /// </summary>
        /// <param name="cameraPos">The camera pos.</param>
        /// <param name="lookDir">The look dir.</param>
        /// <param name="upDir">Up dir.</param>
        /// <param name="fov">The fov.</param>
        /// <param name="znear">The znear.</param>
        /// <param name="zfar">The zfar.</param>
        /// <param name="aspect">The aspect.</param>
        /// <returns>The bounding frustum calculated from perspective camera</returns>
        public static BoundingFrustum FromCamera(Vector3 cameraPos, Vector3 lookDir, Vector3 upDir, float fov, float znear, float zfar, float aspect)
        {
            //http://knol.google.com/k/view-frustum

            lookDir = Vector3.Normalize(lookDir);
            upDir = Vector3.Normalize(upDir);

            Vector3 nearCenter = cameraPos + lookDir * znear;
            Vector3 farCenter = cameraPos + lookDir * zfar;
            float nearHalfHeight = (float)(znear * Math.Tan(fov / 2f));
            float farHalfHeight = (float)(zfar * Math.Tan(fov / 2f));
            float nearHalfWidth = nearHalfHeight * aspect;
            float farHalfWidth = farHalfHeight * aspect;

            Vector3 rightDir = Vector3.Normalize(Vector3.Cross(upDir, lookDir));
            Vector3 Near1 = nearCenter - nearHalfHeight * upDir + nearHalfWidth * rightDir;
            Vector3 Near2 = nearCenter + nearHalfHeight * upDir + nearHalfWidth * rightDir;
            Vector3 Near3 = nearCenter + nearHalfHeight * upDir - nearHalfWidth * rightDir;
            Vector3 Near4 = nearCenter - nearHalfHeight * upDir - nearHalfWidth * rightDir;
            Vector3 Far1 = farCenter - farHalfHeight * upDir + farHalfWidth * rightDir;
            Vector3 Far2 = farCenter + farHalfHeight * upDir + farHalfWidth * rightDir;
            Vector3 Far3 = farCenter + farHalfHeight * upDir - farHalfWidth * rightDir;
            Vector3 Far4 = farCenter - farHalfHeight * upDir - farHalfWidth * rightDir;

            var result = new BoundingFrustum
            {
                NearPlane = new Plane(Near1, Near2, Near3),
                FarPlane = new Plane(Far3, Far2, Far1),
                LeftPlane = new Plane(Near4, Near3, Far3),
                RightPlane = new Plane(Far1, Far2, Near2),
                TopPlane = new Plane(Near2, Far2, Far3),
                BottomPlane = new Plane(Far4, Far1, Near1)
            };

            result.NearPlane.Normalize();
            result.FarPlane.Normalize();
            result.LeftPlane.Normalize();
            result.RightPlane.Normalize();
            result.TopPlane.Normalize();
            result.BottomPlane.Normalize();

            //result.pMatrix = Matrix.LookAtLH(cameraPos, cameraPos + lookDir * 10, upDir) * Matrix.PerspectiveFovLH(fov, aspect, znear, zfar);

            return result;
        }
    }
}
