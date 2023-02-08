using g3;
using Stride.Core;
using Stride.Graphics;
using Stride.Rendering.ProceduralModels;
using System;

namespace VL.Stride.Rendering.Models
{
    /// <summary>
    /// Generates a Box Sphere mesh
    /// </summary>
    [DataContract("BoxSphereMesh")]
    [Display("BoxSphereMesh")] // This name shows up in the procedural model dropdown list
    public class BoxSphereMesh : PrimitiveProceduralModelBase
    {
        /// <summary>
        /// Sphere's radius
        /// </summary>
        [DataMember(10)]
        public float Radius { get; set; } = 0.5f;

        /// <summary>
        /// Sphere's tessellation (amount of vertices). Higher values result in smoother surfaces
        /// </summary>
        [DataMember(11)]
        public int Tessellation { get; set; } = 8;

        /// <summary>
        /// Sphere's vertical anchor position
        /// </summary>
        [DataMember(12)]
        public AnchorMode Anchor { get; set; } = AnchorMode.Middle;

        [DataMember(13)]
        public bool SharedVertices { get; set; } = false;

        [DataMember(14)]
        public bool Clockwise { get; set; } = true;

        /// <summary>
        /// Uses the DMesh3 instance generated from a Sphere3Generator_NormalizedCube to create an equivalent Stride GeometricMeshData<![CDATA[<VertexPositionNormalTexture>]]>
        /// </summary>
        /// <returns>A Stride GeometricMeshData<![CDATA[<VertexPositionNormalTexture>]]> equivalent to the Sphere generated with the public property values</returns>
        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            var generator = new Sphere3Generator_NormalizedCube()
            {
                EdgeVertices = Math.Max(Tessellation + 1, 2),
                Radius = Radius,
                NoSharedVertices = !SharedVertices,
                Clockwise = !Clockwise
            };
            
            return Utils.ToGeometricMeshData(generator.Generate(), "BoxSphereMesh", UvScale, Utils.CalculateYOffset(2* Radius, Anchor) + 0.5f); //Shpere's vertical origin in g3 is offset 0.5 compared to other meshes
        }
    }
}
