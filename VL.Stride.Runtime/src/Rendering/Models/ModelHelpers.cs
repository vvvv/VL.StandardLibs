using System;
using Stride.Core.Mathematics;
using Stride.Rendering;
using StrideModel = Stride.Rendering.Model;

namespace VL.Stride.Rendering
{
    public static class ModelHelpers
    {

        public static StrideModel SetMeshParameter<T>(this StrideModel model, PermutationParameterKey<T> permutationParameter, T value)
        {
            var count = model?.Meshes?.Count;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    model.Meshes[i]?.Parameters?.Set(permutationParameter, value);
                }
            }

            return model;
        }

        /// <summary>
        /// Calculates the vertex normals per triangle. If vertices are shared between triangles, they get an average normal weighted by face size.
        /// From: https://gamedev.stackexchange.com/questions/152991/how-can-i-calculate-normals-using-a-vertex-and-index-buffer
        /// </summary>
        /// <param name="vertexPositions">The vertex positions.</param>
        /// <param name="triangleIndices">The triangle indices.</param>
        /// <param name="vertexNormals">The vertex normals.</param>
        public static void CalculateVertexNormals(Vector3[] vertexPositions, int[] triangleIndices, Vector3[] vertexNormals, bool isLefthanded)
        {

            // Zero-out our normal buffer to start from a clean slate.
            for (int vertex = 0; vertex < vertexPositions.Length; vertex++)
                vertexNormals[vertex] = Vector3.Zero;

            // For each face, compute the face normal, and accumulate it into each vertex.
            var triangleCount = triangleIndices.Length / 3;
            for (int i = 0; i < triangleCount; i++)
            {
                var index = i * 3;
                int vertexA = triangleIndices[index];
                int vertexB = triangleIndices[index + 1];
                int vertexC = triangleIndices[index + 2];

                var edgeAB = vertexPositions[vertexB] - vertexPositions[vertexA];
                var edgeAC = vertexPositions[vertexC] - vertexPositions[vertexA];

                // The cross product is perpendicular to both input vectors (normal to the plane).
                // Flip the argument order if you need the opposite winding.    
                var areaWeightedNormal = isLefthanded ? Vector3.Cross(edgeAB, edgeAC) : -Vector3.Cross(edgeAB, edgeAC);

                // Don't normalize this vector just yet. Its magnitude is proportional to the
                // area of the triangle (times 2), so this helps ensure tiny/skinny triangles
                // don't have an outsized impact on the final normal per vertex.

                // Accumulate this cross product into each vertex normal slot.
                vertexNormals[vertexA] += areaWeightedNormal;
                vertexNormals[vertexB] += areaWeightedNormal;
                vertexNormals[vertexC] += areaWeightedNormal;
            }

            // Finally, normalize all the sums to get a unit-length, area-weighted average.
            for (int vertex = 0; vertex < vertexPositions.Length; vertex++)
                vertexNormals[vertex].Normalize();
        }
    }
}
