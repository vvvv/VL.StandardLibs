using Stride.Assets.Models;
using Stride.Core;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Runtime.CompilerServices;
using StrideModel = Stride.Rendering.Model;
using VL.Stride.Utils;

namespace VL.Stride.Rendering
{

    public static class ModelHelpers
    {
        class CommandContext : ICommandContext
        {
            public Command CurrentCommand => throw new NotImplementedException();

            public LoggerResult Logger => null;

            public void AddTag(ObjectUrl url, string tag)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IReadOnlyDictionary<ObjectUrl, OutputObject>> GetOutputObjectsGroups()
            {
                throw new NotImplementedException();
            }

            public void RegisterCommandLog(IEnumerable<ILogMessage> logMessages)
            {
                throw new NotImplementedException();
            }

            public void RegisterInputDependency(ObjectUrl url)
            {
                throw new NotImplementedException();
            }

            public void RegisterOutput(ObjectUrl url, ObjectId hash)
            {
                throw new NotImplementedException();
            }
        }

        internal static StrideModel LoadModel(string filePath, float importScale, Vector3 pivotPosition, bool mergeMeshes, IGraphicsDeviceService graphicsDeviceService)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            var command = ImportModelCommand.Create(Path.GetExtension(filePath));
            if (command is null)
                return null;

            command.Mode = ImportModelCommand.ExportMode.Model;
            command.SourcePath = filePath?.CreateUFile(); // Takes care of network paths
            command.ScaleImport = importScale;
            command.PivotPosition = pivotPosition;
            command.MaxInputSlots = 32;
            command.Allow32BitIndex = true;
            command.Materials = new List<ModelMaterial>();
            command.MergeMeshes = mergeMeshes;
            command.DeduplicateMaterials = true;
            command.Location = new UFile(Guid.NewGuid().ToString());

            using var inMemoryFileProvider = new MemoryFileProvider("/tmp/in-memory-database");
            using var objectDatabase = new ObjectDatabase(inMemoryFileProvider.RootPath, "temp-index", loadDefaultBundle: false);
            using var fileProvider = new DatabaseFileProvider(objectDatabase);
            var services = new ServiceRegistry();
            services.AddService<IDatabaseFileProviderService>(new DatabaseFileProviderService(fileProvider));
            services.AddService(graphicsDeviceService);
            var contentManager = new ContentManager(services);

            var rawModel = command.ExportModel(new CommandContext(), contentManager);
            // The returned model is not useable for runtime (buffers are not attached to graphics device)
            // We need to serialize/deserialize for the to get connected
            contentManager.Save(command.Location, rawModel);
            return contentManager.Load<StrideModel>(command.Location);
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(ExportModel))]
        extern static object ExportModel(this ImportModelCommand command, ICommandContext commandContext, ContentManager contentManager);

        internal static void ReleaseGraphicsResources(this StrideModel model)
        {
            foreach (var mesh in model.Meshes)
                mesh.ReleaseGraphicsResources();
        }

        internal static void ReleaseGraphicsResources(this Mesh mesh)
        {
            if (mesh.Draw is null)
                return;

            mesh.Draw.IndexBuffer?.Buffer?.Dispose();
            foreach (var v in mesh.Draw.VertexBuffers)
                v.Buffer?.Dispose();
        }

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
        /// <param name="isLefthanded"></param>
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
