using g3;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.ProceduralModels;
using System;

namespace VL.Stride.Rendering.Models
{
    /// <summary>
    /// Generates a Cone mesh
    /// </summary>
    [DataContract("ConeMesh")]
    [Display("ConeMesh")] // This name shows up in the procedural model dropdown list
    public class ConeMesh : PrimitiveProceduralModelBase
    {
        /// <summary>
        /// Cone's height
        /// </summary>
        [DataMember(10)]
        public float Height { get; set; } = 1;

        /// <summary>
        /// Cone's radius
        /// </summary>
        [DataMember(11)]
        public float Radius { get; set; } = 0.5f;

        /// <summary>
        /// Cone's initial angle in cycles 
        /// </summary>
        [DataMember(12)]
        public float FromAngle { get; set; } = 0f;

        /// <summary>
        /// Cone's final angle in cycles
        /// </summary>
        [DataMember(13)]
        public float ToAngle { get; set; } = 1f;

        /// <summary>
        /// Boolean value indicating if the cone should have a bottom cap
        /// </summary>
        [DataMember(14)]
        public bool Capped { get; set; } = true;

        /// <summary>
        /// Determines if the cone's back face should be generated or not
        /// </summary>
        [DataMember(15)]
        public bool GenerateBackFace { get; set; } = false;

        /// <summary>
        /// Cone's tessellation (amount of radial and of vertical slices to split the cone into). Higher values result in smoother surfaces
        /// </summary>
        [DataMember(16)]
        public Int2 Tessellation { get; set; } = new Int2(16, 1);

        /// <summary>
        /// Cone's vertical anchor position
        /// </summary>
        [DataMember(17)]
        public AnchorMode Anchor { get; set; } = AnchorMode.Middle;

        [DataMember(18)]
        public SlopeUVMode SlopeUVMode { get; set; } = SlopeUVMode.SideProjected;

        /* TODO: Implement UV/Normals properly and expose
        [DataMember(17)]
        public bool SharedVertices { get; set; } = false;
        */

        [DataMember(18)]
        public bool Clockwise { get; set; } = false;

        /// <summary>
        /// Uses the DMesh3 instance generated from a ConeGenerator to create an equivalent Stride GeometricMeshData<![CDATA[<VertexPositionNormalTexture>]]>
        /// </summary>
        /// <returns>A Stride GeometricMeshData<![CDATA[<VertexPositionNormalTexture>]]> equivalent to the Cone generated with the public property values</returns>
        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            bool closed = (1 - FromAngle) - (1 - ToAngle) == 1;
            var generator = new ConeGenerator
            {
                BaseRadius = Radius,
                EndAngleDeg = (1 - FromAngle) * 360,
                StartAngleDeg = (1 - ToAngle) * 360,
                Height = Height,
                Capped = Capped,
                GenerateBackFace = GenerateBackFace,
                Slices = closed ? Math.Max(Tessellation.X, 2) : Math.Max(Tessellation.X + 1, 2),
                Rings = Math.Max(Tessellation.Y + 1, 2),
                SlopeUVMode = SlopeUVMode == SlopeUVMode.OnShape ? g3.SlopeUVMode.OnShape : g3.SlopeUVMode.SideProjected,
                NoSharedVertices = true,
                Clockwise = Clockwise,
                AddSliceWhenOpen = true
            };

            return Utils.ToGeometricMeshData(generator.Generate(), "ConeMesh", UvScale, Utils.CalculateYOffset(Height, Anchor));
        }
    }

    public enum SlopeUVMode
    {
        OnShape,
        SideProjected
    }
}
