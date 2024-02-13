using Stride.Graphics;
using Stride.Core.Mathematics;
using Buffer = Stride.Graphics.Buffer;

namespace VL.ImGui
{
    partial class ImGuiRenderer
    {
        void CreateDeviceObjects()
        {
            // set up a commandlist
            commandList = context.CommandList;



            var layout = new VertexDeclaration(
                VertexElement.Position<Vector2>(),
                VertexElement.TextureCoordinate<Vector2>(),
                VertexElement.Color(PixelFormat.R8G8B8A8_UNorm)
            );

            imVertLayout = layout;

            // pipeline desc
            var pipeline = new PipelineStateDescription()
            {
                BlendState = BlendStates.NonPremultiplied,

                RasterizerState = new RasterizerStateDescription()
                {
                    CullMode = CullMode.None,
                    DepthBias = 0,
                    FillMode = FillMode.Solid,
                    MultisampleAntiAliasLine = false,
                    ScissorTestEnable = true,
                    SlopeScaleDepthBias = 0,
                },

                PrimitiveType = PrimitiveType.TriangleList,
                InputElements = imVertLayout.CreateInputElements(),
                DepthStencilState = DepthStencilStates.Default,

                EffectBytecode = imShader.EffectInstance.Effect.Bytecode,
                RootSignature = imShader.EffectInstance.RootSignature,

                Output = new RenderOutputDescription(PixelFormat.R8G8B8A8_UNorm)
            };


            // finally set up the pipeline
            var pipelineState = PipelineState.New(device, ref pipeline);
            imPipeline = pipelineState;


            // Setup Buffers
            var is32Bits = false;
            var indexBuffer = Buffer.Index.New(device, INITIAL_INDEX_BUFFER_SIZE * sizeof(ushort), GraphicsResourceUsage.Dynamic);
            var indexBufferBinding = new IndexBufferBinding(indexBuffer, is32Bits, 0);
            indexBinding = indexBufferBinding;

            var vertexBuffer = Buffer.Vertex.New(device, INITIAL_VERTEX_BUFFER_SIZE * imVertLayout.CalculateSize(), GraphicsResourceUsage.Dynamic);
            var vertexBufferBinding = new VertexBufferBinding(vertexBuffer, layout, 0);
            vertexBinding = vertexBufferBinding;
        }
    }
}
