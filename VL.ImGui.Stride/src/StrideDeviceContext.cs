using ImGuiNET;
using Stride.Input;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Core.Mathematics;
using Buffer = Stride.Graphics.Buffer;

using VL.Core;
using VL.Lib.Collections;
using VL.Stride;

using VL.Lib.Basics.Resources;
using VL.ImGui.Stride.Effects;
using Stride.Shaders.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VL.ImGui
{
    using ImGui = ImGuiNET.ImGui;
    internal sealed partial class StrideDeviceContext : StrideContext
    {
        const int INITIAL_VERTEX_BUFFER_SIZE = 128;
        const int INITIAL_INDEX_BUFFER_SIZE = 128;

        // dependencies
        private readonly IResourceHandle<GraphicsDevice> deviceHandle;
        private readonly IResourceHandle<GraphicsContext> GraphicsContextHandle;
        private readonly IResourceHandle<InputManager> inputHandle;

        GraphicsDevice device => deviceHandle.Resource;
        GraphicsContext graphicsContext => GraphicsContextHandle.Resource;
        InputManager inputManager => inputHandle.Resource;

        public IEnumerable<UniqueId> path => nodeContext.Path.Stack.SkipLast(1);

        public ImGuiIOPtr IO => _io;

        // ImGui
        private readonly ImGuiIOPtr _io;
        private float _fontScaling;
        private float _uiScaling;
        private Spread<FontConfig?> _fonts = Spread.Create(FontConfig.Default);

        // Stride
        private PipelineState imPipeline;
        private VertexDeclaration imVertLayout;
        private VertexBufferBinding vertexBinding;
        private IndexBufferBinding indexBinding;
        private readonly EffectInstance imShader;
        private Texture? fontTexture;

        //VL 
        private NodeContext nodeContext;

        // Flag to detect redundant calls
        private bool disposed = false;

        public unsafe StrideDeviceContext(NodeContext nodeContext) : base()
        {
            this.nodeContext = nodeContext;

            deviceHandle = nodeContext.AppHost.Services.GetDeviceHandle();
            GraphicsContextHandle = nodeContext.AppHost.Services.GetGraphicsContextHandle();
            inputHandle = nodeContext.AppHost.Services.GetInputManagerHandle();

            using var gameHandle = nodeContext.AppHost.Services.GetGameHandle();
            var effectSystem = gameHandle.Resource.EffectSystem;

            var compilerParameters = new CompilerParameters();
            compilerParameters.Set(ImGuiEffectShaderKeys.ColorIsSRgb, device.ColorSpace == ColorSpace.Linear);
            imShader = new EffectInstance(effectSystem.LoadEffect("ImGuiEffect", compilerParameters).WaitForResult());
            imShader.UpdateEffect(device);

            using (this.MakeCurrent())
            {
                _io = ImGui.GetIO();
                _io.NativePtr->IniFilename = null;

                #region DeviceObjects

                // set VertexLayout
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

                    EffectBytecode = imShader.Effect.Bytecode,
                    RootSignature = imShader.RootSignature,

                    Output = new RenderOutputDescription(PixelFormat.R8G8B8A8_UNorm)
                };

                // finally set up the pipeline
                var pipelineState = PipelineState.New(device, ref pipeline);
                imPipeline = pipelineState;

                // Setup Buffers
                var is32Bits = false;
                var indexBuffer = Buffer.Index.New(device, INITIAL_INDEX_BUFFER_SIZE * sizeof(ushort), GraphicsResourceUsage.Default);
                var indexBufferBinding = new IndexBufferBinding(indexBuffer, is32Bits, 0);
                indexBinding = indexBufferBinding;

                var vertexBuffer = Buffer.Vertex.New(device, INITIAL_VERTEX_BUFFER_SIZE * imVertLayout.CalculateSize(), GraphicsResourceUsage.Default);
                var vertexBufferBinding = new VertexBufferBinding(vertexBuffer, layout, 0);
                vertexBinding = vertexBufferBinding;
                #endregion DeviceObjects

                var scaling = VL.UI.Core.DIPHelpers.DIPFactor();
                UpdateScaling(fontScaling: scaling, uiScaling: scaling);
            }
        }

        #region scaling
        void UpdateScaling(float fontScaling, float uiScaling)
        {
            if (fontScaling != _fontScaling)
            {
                _fontScaling = fontScaling;
                BuildImFontAtlas(device, _io.Fonts, _fontScaling);
            }
            if (uiScaling != _uiScaling)
            {
                _uiScaling = uiScaling;
                RenderHelper.UpdateUIScaling(uiScaling);
            }
        }
        #endregion scaling

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    imPipeline.Dispose();
                    vertexBinding.Buffer.Dispose();
                    indexBinding.Buffer.Dispose();
                    fontTexture?.Dispose();
                    imShader.Dispose();

                    deviceHandle.Dispose();
                    GraphicsContextHandle.Dispose();
                    inputHandle.Dispose();
                }

                // Dispose unmanaged resources

                disposed = true;
            }

            // Call base class implementation
            base.Dispose(disposing);
        }

        ~StrideDeviceContext()
        {
            Dispose(false);
        }
    }
}
