using g3;
using Stride.Core;
using Stride.Graphics;
using Stride.Rendering.ProceduralModels;
using System;

namespace VL.Stride.Rendering.Models
{
    /// <summary>
    /// Generates a radial 3D Arrow mesh
    /// </summary>
    [DataContract("ArrowMesh")]
    [Display("ArrowMesh")] // This name shows up in the procedural model dropdown list
    public class ArrowMesh : PrimitiveProceduralModelBase
    {
        /// <summary>
        /// Arrow's stick length
        /// </summary>
        [DataMember(10)]
        public float StickLength { get; set; } = 1f;

        /// <summary>
        /// Arrow's stick radius
        /// </summary>
        [DataMember(11)]
        public float StickRadius { get; set; } = 0.125f;

        /// <summary>
        /// Arrow's head length
        /// </summary>
        [DataMember(12)]
        public float HeadLength { get; set; } = 0.5f;

        /// <summary>
        /// Arrow's head radius
        /// </summary>
        [DataMember(13)]
        public float HeadRadius { get; set; } = 0.25f;

        /// <summary>
        /// Arrow's tip radius
        /// </summary>
        [DataMember(14)]
        public float TipRadius { get; set; } = 0f;

        /// <summary>
        /// Arrow's tessellation (amount of radial slices to split the arrow into). Higher values result in smoother surfaces.
        /// </summary>
        [DataMember(15)]
        public int Tessellation { get; set; } = 16;

        /// <summary>
        /// Arrow's vertical anchor position
        /// </summary>
        [DataMember(16)]
        public AnchorMode Anchor { get; set; } = AnchorMode.Middle;

        [DataMember(17)]
        public bool SharedVertices { get; set; } = false;

        [DataMember(18)]
        public bool Clockwise { get; set; } = false;

        /// <summary>
        /// Uses the DMesh3 instance generated from a Radial3DArrowGenerator to create an equivalent Stride GeometricMeshData<![CDATA[<VertexPositionNormalTexture>]]>
        /// </summary>
        /// <returns>A Stride GeometricMeshData<![CDATA[<VertexPositionNormalTexture>]]> equivalent to the ArrowMesh generated with the public property values</returns>
        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {

            var generator = new Radial3DArrowGenerator
            {
                HeadBaseRadius = HeadRadius,
                HeadLength = HeadLength,
                StickLength = StickLength,
                StickRadius = StickRadius,
                TipRadius = TipRadius,
                Slices = Math.Max(Tessellation, 2),
                NoSharedVertices = !SharedVertices,
                Clockwise = Clockwise
            };

            return Utils.ToGeometricMeshData(generator.Generate(), "ArrowMesh", UvScale, Utils.CalculateYOffset(HeadLength + StickLength, Anchor));
        }
    }
}
