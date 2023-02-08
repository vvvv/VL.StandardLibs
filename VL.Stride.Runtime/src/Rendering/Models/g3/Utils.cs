using g3;
using Stride.Core.Mathematics;
using Stride.Graphics;
using System.Collections.Generic;

namespace VL.Stride.Rendering.Models
{
    /// <summary>
    /// Utility class to aid with G3 to Stride conversions
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Uses SimpleMesh's vertex, normal and UV data to generate a stride equivalent
        /// </summary>
        /// <param name="meshGenerator">A g3 SimpleMesh instance</param>
        /// <param name="name">The model name</param>
        /// <param name="UVScale">UV scale factor as a Vector2</param>
        /// <returns>An equivalent Stride GeometricMeshData</returns>
        public static GeometricMeshData<VertexPositionNormalTexture> ToGeometricMeshData(MeshGenerator meshGenerator, string name, Vector2 UVScale, float yOffset = 0f)
        {
            if (meshGenerator is null || meshGenerator.vertices.Count == 0)
                return null;

            var vertexCount = meshGenerator.vertices.Count;
            var vertices = new VertexPositionNormalTexture[vertexCount];

            for (int i = 0; i < vertexCount; i++)
            {
                var normal = new Vector3(meshGenerator.normals[i].x, meshGenerator.normals[i].y, meshGenerator.normals[i].z);
                var uv = new Vector2(meshGenerator.uv[i].x, 1 - meshGenerator.uv[i].y) * UVScale;
                vertices[i] = new VertexPositionNormalTexture(new Vector3((float)meshGenerator.vertices[i].x, (float)meshGenerator.vertices[i].y + yOffset, (float)meshGenerator.vertices[i].z), normal, uv);
            }

            return new GeometricMeshData<VertexPositionNormalTexture>(vertices, meshGenerator.triangles.array, isLeftHanded: true) { Name = name };
        }

        /// <summary>
        /// Converts a Stride Vector3 to a geomtry3Sharp Vector3d
        /// </summary>
        /// <param name="vector">Stride Vector3</param>
        /// <returns>A geomtry3Sharp Vector3d</returns>
        public static Vector3d ToVector3d(Vector3 vector)
        {
            if (vector != null)
                return new Vector3d(vector.X, vector.Y, vector.Z);
            return Vector3d.Zero;
        }

        /// <summary>
        /// Converts an IReadOnlyList of Stride Vector3 to an IReadOnlyList of geomtry3Sharp Vector3d
        /// </summary>
        /// <param name="vectors">IReadOnlyList of Stride Vector3</param>
        /// <returns>An equivalent IReadOnlyList of geomtry3Sharp Vector3d</returns>
        public static IReadOnlyList<Vector3d> ToVector3dList(IReadOnlyList<Vector3> vectors)
        {
            if (vectors != null && vectors.Count > 0)
            {
                var result = new Vector3d[vectors.Count];
                for (int i = 0; i < vectors.Count; i++)
                {
                    result[i] = ToVector3d(vectors[i]);
                }
                return result;
            }
            return null;
        }

        /// <summary>
        /// Converts a Stride Vector2 to a geomtry3Sharp Vector2d
        /// </summary>
        /// <param name="vector">Stride Vector2</param>
        /// <returns>A geomtry3Sharp Vector2d</returns>
        public static Vector2d ToVector2d(Vector2 vector)
        {
            if (vector != null)
                return new Vector2d(vector.X, vector.Y);
            return Vector2d.Zero;
        }

        /// <summary>
        /// Converts an IReadOnlyList of Stride Vector2 to an IReadOnlyList of geomtry3Sharp Vector2d
        /// </summary>
        /// <param name="vectors">IReadOnlyList of Stride Vector2</param>
        /// <returns>An equivalent IReadOnlyList of geomtry3Sharp Vector2d</returns>
        public static IReadOnlyList<Vector2d> ToVector2dList(IReadOnlyList<Vector2> vectors)
        {
            if (vectors != null && vectors.Count > 0)
            {
                var result = new Vector2d[vectors.Count];
                for (int i = 0; i < vectors.Count; i++)
                {
                    result[i] = ToVector2d(vectors[i]);
                }
                return result;
            }
            return null;
        }

        /// <summary>
        /// Converts a VL.Stride.Rendering.Models.Corner to a g3.RoundRectGenerator.Corner
        /// </summary>
        /// <param name="corner">A VL.Stride.Rendering.Models.RoundRectModel.Corner</param>
        /// <returns>A g3.RoundRectGenerator.Corner</returns>
        public static RoundRectGenerator.Corner ToCorner(RoundRectangleMesh.SharpCorner corner)
        {
            return (RoundRectGenerator.Corner)((byte)corner);
        }

        /// <summary>
        /// Calculates the approriate Y offset for a mesh with the specified height and anchor mode
        /// </summary>
        /// <param name="height">Mesh height</param>
        /// <param name="anchor">Anchor mode to be used</param>
        /// <returns>The approriate Y offset for a mesh with the specified height and anchor mode</returns>
        public static float CalculateYOffset(float height, AnchorMode anchor)
        {
            switch (anchor)
            {
                case AnchorMode.Top:
                    return -height;
                case AnchorMode.Middle:
                    return (height / -2f);
                case AnchorMode.Bottom:
                default:
                    return 0f;
            }
        }
    }
}
