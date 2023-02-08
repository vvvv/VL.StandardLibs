using g3;
using Stride.Core;
using Stride.Graphics;
using Stride.Rendering.ProceduralModels;
using System;
using System.Collections.Generic;

namespace VL.Stride.Rendering.Models
{
    /// <summary>
    /// Generates a Vertical Generalized Cylinder mesh, described by multiple concentric circular sections at different distances in the Y axis
    /// </summary>
    [DataContract("VerticalGeneralizedCylinderMesh")]
    [Display("VerticalGeneralizedCylinderMesh")] // This name shows up in the procedural model dropdown list
    public class VerticalGeneralizedCylinderMesh : PrimitiveProceduralModelBase
    {
        /// <summary>
        /// Boolean value indicating if the cylinder should have caps
        /// </summary>
        [DataMember(10)]
        public bool Capped { get; set; } = true;

        /// <summary>
        /// IReadOnlyList of circular sections that make up the cylinder
        /// </summary>
        [DataMember(11)]
        public IReadOnlyList<MeshGenerator.CircularSection> Sections { get; set; }

        /// <summary>
        /// Cylinder's tessellation (amount of radial slices to split the cylinder into). Higher values result in smoother surfaces
        /// </summary>
        [DataMember(12)]
        public int Tessellation { get; set; } = 16;

        /// <summary>
        /// Cylinder's vertical anchor position
        /// </summary>
        [DataMember(13)]
        public AnchorMode Anchor { get; set; } = AnchorMode.Middle;

        [DataMember(14)]
        public bool SharedVertices { get; set; } = false;

        [DataMember(15)]
        public bool Clockwise { get; set; } = false;

        /// <summary>
        /// Uses the DMesh3 instance generated from a VerticalGeneralizedCylinderGenerator to create an equivalent Stride GeometricMeshData<![CDATA[<VertexPositionNormalTexture>]]>
        /// </summary>
        /// <returns>A Stride GeometricMeshData<![CDATA[<VertexPositionNormalTexture>]]> equivalent to the VerticalGeneralizedCylinder generated with the public property values</returns>
        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            MeshGenerator.CircularSection[] output;
            Sections.TryGetArray(out output);
            var generator = new VerticalGeneralizedCylinderGenerator
            {
                Capped = Capped,
                Sections = output,
                Slices = Math.Max(Tessellation, 2),
                NoSharedVertices = !SharedVertices,
                Clockwise = Clockwise
            };

            float minHeight = 0f;
            float maxHeight = 0f;
            foreach (var s in Sections)
            {
                if (s.SectionY > maxHeight)
                {
                    maxHeight = s.SectionY;
                    continue;
                }
                if (s.SectionY < minHeight)
                {
                    minHeight = s.SectionY;
                }
            }

            return Utils.ToGeometricMeshData(generator.Generate(), "VerticalGeneralizedCylinderMesh", UvScale, Utils.CalculateYOffset(maxHeight - minHeight, Anchor));
        }
    }
}
