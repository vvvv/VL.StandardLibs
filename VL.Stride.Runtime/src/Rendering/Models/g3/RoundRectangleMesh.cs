using g3;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.ProceduralModels;
using NormalDirection = Stride.Graphics.GeometricPrimitives.NormalDirection;

namespace VL.Stride.Rendering.Models
{
    /// <summary>
    /// Generates a Rounded Rectangle mesh
    /// </summary>
    [DataContract("RoundRectangleMesh")]
    [Display("RoundRectangleMesh")] // This name shows up in the procedural model dropdown list
    public class RoundRectangleMesh : PrimitiveProceduralModelBase
    {
        /// <summary>
        /// RoundRectangle's size as a 2D vector
        /// </summary>
        [DataMember(10)]
        public Vector2 Size { get; set; } = Vector2.One;

        /// <summary>
        /// RoundRectangle's corner radius
        /// </summary>
        [DataMember(11)]
        public float Radius { get; set; } = 0.25f;

        /// <summary>
        /// RoundRectangle's configurable sharp corners. Use the SharpCorner enum's OR operator to configure multiple sharp corners at once
        /// </summary>
        [DataMember(12)]
        public SharpCorner SharpCorners { get; set; } = SharpCorner.None;


        /// <summary>
        /// RoundRectangle's amount of steps per corner
        /// </summary>
        [DataMember(13)]
        public int CornerTessellation { get; set; } = 4;

        /// <summary>
        /// RoundRectangle's axis to use as the Up vector
        /// </summary>
        [DataMember(14)]
        public NormalDirection Normal = NormalDirection.UpZ;

        /// <summary>
        /// Determines if roundRectangle's back face should be generated or not
        /// </summary>
        [DataMember(15)]
        public bool GenerateBackFace = true;

        [DataMember(16)]
        public bool Clockwise { get; set; } = false;

        /// <summary>
        /// Uses the DMesh3 instance generated from a RoundRectGenerator to create an equivalent Stride GeometricMeshData<![CDATA[<VertexPositionNormalTexture>]]>
        /// </summary>
        /// <returns>A Stride GeometricMeshData<![CDATA[<VertexPositionNormalTexture>]]> equivalent to the RoundRect generated with the public property values</returns>
        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            g3.NormalDirection normal;

            switch (Normal)
            {
                default:
                case NormalDirection.UpY: normal = g3.NormalDirection.UpY; break;
                case NormalDirection.UpZ: normal = g3.NormalDirection.UpZ; break;
                case NormalDirection.UpX: normal = g3.NormalDirection.UpX; break;
            }

            var generator = new RoundRectGenerator
            {
                CornerSteps = CornerTessellation,
                Width = Size.X,
                Height = Size.Y,
                Radius = Radius,
                SharpCorners = Utils.ToCorner(SharpCorners),
                TextureSpace = TextureSpace.DirectX,
                Normal = normal,
                GenerateBackFace = GenerateBackFace,
                Clockwise = !Clockwise
            };

            return Utils.ToGeometricMeshData(generator.Generate(), "RoundRectMesh", UvScale);
        }

        /// <summary>
        /// Enum to address the individual corner of a RoundRectModel
        /// </summary>
        /// Top-bottom and Left-right are inverted in respect to Stride (mesh is looking down) hence the order/value change
        public enum SharpCorner
        {
            None = 0,
            TopLeft = 1,
            TopRight = 2,
            BottomLeft = 8,
            BottomRight = 4
        }
    }
}
