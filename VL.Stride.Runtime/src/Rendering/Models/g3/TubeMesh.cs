using g3;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.ProceduralModels;
using System.Collections.Generic;

namespace VL.Stride.Rendering.Models
{
    /// <summary>
    /// Generates a Tube mesh of configurable shape that follows a 3D path
    /// </summary>
    [DataContract("TubeMesh")]
    [Display("TubeMesh")] // This name shows up in the procedural model dropdown list
    public class TubeMesh : PrimitiveProceduralModelBase
    {
        /// <summary>
        /// Tube's path as an IReadOnlyList of 3D vectors
        /// </summary>
        [DataMember(10)]
        public IReadOnlyList<Vector3> Path { get; set; }

        /// <summary>
        /// Boolean value indicating if the tube's path should be a closed loop
        /// </summary>
        [DataMember(11)]
        public bool Closed { get; set; }

        /// <summary>
        /// Tube's shape as an IReadOnlyList of 2D vectors
        /// </summary>
        [DataMember(12)]
        public IReadOnlyList<Vector2> Shape { get; set; }

        /// <summary>
        /// Boolean value indicating if the tube should have caps
        /// </summary>
        [DataMember(13)]
        public bool Capped { get; set; }

        [DataMember(14)]
        public bool SharedVertices { get; set; } = false;

        [DataMember(15)]
        public bool Clockwise { get; set; } = true;

        /// <summary>
        /// Uses the DMesh3 instance generated from a TubeGenerator to create an equivalent Stride GeometricMeshData<![CDATA[<VertexPositionNormalTexture>]]>
        /// </summary>
        /// <returns>A Stride GeometricMeshData<![CDATA[<VertexPositionNormalTexture>]]> equivalent to the Tube generated with the public property values</returns>
        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            if (Path != null && Path.Count > 0)
            {
                var path = new DCurve3(Utils.ToVector3dList(Path), Closed);
                var tubeShape = new Polygon2d(Utils.ToVector2dList(Shape));

                var generator = new TubeGenerator(path, tubeShape)
                {
                    Capped = Capped,
                    NoSharedVertices = !SharedVertices,
                    Clockwise = !Clockwise
                };

                return Utils.ToGeometricMeshData(generator.Generate(), "TubeMesh", UvScale);
            }
            return new GeometricMeshData<VertexPositionNormalTexture>(new VertexPositionNormalTexture[0], new int[0], false) { Name = "TubeMesh" };
        }
    }
}
