using System;
using System.Reflection;
using Stride.Core.Mathematics;
using Stride.Rendering;

namespace VL.Stride.Rendering
{
    public static class MeshExtensions
    {
        public static Mesh CloneWithNewParameters(this Mesh mesh)
        {
            var newMesh = new Mesh(mesh);
            newMesh.ReplaceParameters();
            return newMesh;
        }

        public static Mesh ReplaceParameters(this Mesh mesh)
        {
            var p = typeof(Mesh).GetProperty(nameof(Mesh.Parameters), BindingFlags.Public | BindingFlags.Instance);
            p.SetValue(mesh, new ParameterCollection(mesh.Parameters));
            return mesh;
        }
    }
}
