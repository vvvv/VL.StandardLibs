using g3;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.ProceduralModels;
using System;

namespace VL.Stride.Rendering.Models
{
    /// <summary>
    /// Generates a Cylinder mesh
    /// </summary>
    [DataContract("CylinderMesh")]
    [Display("CylinderMesh")] // This name shows up in the procedural model dropdown list
    public class CylinderMesh : PrimitiveProceduralModelBase
    {
        /// <summary>
        /// Cylinder's height
        /// </summary>
        [DataMember(10)]
        public float Height { get; set; } = 1;

        /// <summary>
        /// Cylinder's base radius
        /// </summary>
        [DataMember(11)]
        public float BaseRadius { get; set; } = 0.5f;

        /// <summary>
        /// Cylinder's top radius
        /// </summary>
        [DataMember(12)]
        public float TopRadius { get; set; } = 0.5f;

        /// <summary>
        /// Cylinder's initial angle in cycles 
        /// </summary>
        [DataMember(13)]
        public float FromAngle { get; set; } = 0f;

        /// <summary>
        /// Cylinder's final angle in cycles
        /// </summary>
        [DataMember(14)]
        public float ToAngle { get; set; } = 1f;

        /// <summary>
        /// Boolean value indicating if the cylinder should have caps
        /// </summary>
        [DataMember(15)]
        public bool Capped { get; set; } = true;

        /// <summary>
        /// Determines if the cylinder's back face should be generated or not
        /// </summary>
        [DataMember(16)]
        public bool GenerateBackFace { get; set; } = false;

        /// <summary>
        /// Cylinder's tessellation (amount of radial and of vertical slices to split the cylinder into). Higher values result in smoother surfaces
        /// </summary>
        [DataMember(17)]
        public Int2 Tessellation { get; set; } = new Int2(16, 1);

        /// <summary>
        /// Cylinder's vertical anchor position
        /// </summary>
        [DataMember(18)]
        public AnchorMode Anchor { get; set; } = AnchorMode.Middle;

        /* TODO: Implement UV/Normals properly and expose
        [DataMember(18)]
        public bool SharedVertices { get; set; } = false;
        */

        [DataMember(19)]
        public bool Clockwise { get; set; } = false;

        /// <summary>
        /// Uses the DMesh3 instance generated from a OpenCylinderGenerator or CappedCylinderGenerator to create an equivalent Stride GeometricMeshData<![CDATA[<VertexPositionNormalTexture>]]>
        /// </summary>
        /// <returns>A Stride GeometricMeshData<![CDATA[<VertexPositionNormalTexture>]]> equivalent to the Cylinder generated with the public property values</returns>
        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            bool closed = (1 - FromAngle) - (1 - ToAngle) == 1;
            MeshGenerator generator;
            if (Capped)
            {
                generator = new CappedCylinderGenerator
                {
                    BaseRadius = BaseRadius,
                    TopRadius = TopRadius,
                    StartAngleDeg = (1 - ToAngle) * 360,
                    EndAngleDeg = (1 - FromAngle) * 360,
                    Height = Height,
                    Slices = closed ? Math.Max(Tessellation.X, 2) : Math.Max(Tessellation.X + 1, 2),
                    Rings = Math.Max(Tessellation.Y + 1, 2),
                    NoSharedVertices = true,
                    Clockwise = Clockwise,
                    AddSliceWhenOpen = true
                };
            }
            else
            {
                generator = new OpenCylinderGenerator
                {
                    BaseRadius = BaseRadius,
                    TopRadius = TopRadius,
                    StartAngleDeg = (1 - ToAngle) * 360,
                    EndAngleDeg = (1 - FromAngle) * 360,
                    Height = Height,
                    GenerateBackFace = GenerateBackFace,
                    Slices = closed ? Math.Max(Tessellation.X, 2) : Math.Max(Tessellation.X + 1, 2),
                    Rings = Math.Max(Tessellation.Y + 1, 2),
                    NoSharedVertices = true,
                    Clockwise = Clockwise,
                    AddSliceWhenOpen = true
                };
            }

            return Utils.ToGeometricMeshData(generator.Generate(), "CylinderMesh", UvScale, Utils.CalculateYOffset(Height, Anchor));
        }
    }
}
