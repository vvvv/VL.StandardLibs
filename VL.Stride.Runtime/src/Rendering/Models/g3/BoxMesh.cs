using g3;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.ProceduralModels;
using System;

namespace VL.Stride.Rendering.Models
{
    /// <summary>
    /// Generates a Box mesh
    /// </summary>
    [DataContract("BoxMesh")]
    [Display("BoxMesh")] // This name shows up in the procedural model dropdown list
    public class BoxMesh : PrimitiveProceduralModelBase
    {
        /// <summary>
        /// Box's tessellation (amount of vertices per edge)
        /// </summary>
        [DataMember(10)]
        public Vector3 Size { get; set; } = Vector3.One;

        /// <summary>
        /// Box's tessellation (amount of vertices per edge)
        /// </summary>
        [DataMember(10)]
        public int Tessellation { get; set; } = 1;

        /// <summary>
        /// Box's vertical anchor position
        /// </summary>
        [DataMember(11)]
        public AnchorMode Anchor { get; set; } = AnchorMode.Middle;

        [DataMember(12)]
        public bool SharedVertices { get; set; } = false;

        [DataMember(13)]
        public bool Clockwise { get; set; } = true;


        /// <summary>
        /// Uses the DMesh3 instance generated from a GridBox3Generator to create an equivalent Stride GeometricMeshData<![CDATA[<VertexPositionNormalTexture>]]>
        /// </summary>
        /// <returns>A Stride GeometricMeshData<![CDATA[<VertexPositionNormalTexture>]]> equivalent to the GridBox generated with the public property values</returns>
        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            var generator = new GridBox3Generator
            {
                Box = new Box3d(new Vector3d(0, 0, 0), new Vector3d(Size.X / 2, Size.Y / 2, Size.Z / 2)),
                EdgeVertices = Math.Max(Tessellation + 1, 2),
                NoSharedVertices = !SharedVertices,
                Clockwise = !Clockwise
            };

            return Utils.ToGeometricMeshData(generator.Generate(), "BoxMesh", UvScale, Utils.CalculateYOffset(1f, Anchor) + 0.5f);//GridBox's vertical origin in g3 is offset 0.5 compared to other meshes
        }
    }
}
