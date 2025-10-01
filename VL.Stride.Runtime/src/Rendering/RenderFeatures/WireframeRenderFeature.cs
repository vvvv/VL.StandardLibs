using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace VL.Stride.Rendering
{
    public class WireframeRenderFeature : RootRenderFeature
    {
        DynamicEffectInstance shader;
        MutablePipelineState pipelineState;
        private static readonly ProfilingKey DrawKey = new ProfilingKey("SinglePassWireframeRenderFeature.Draw");

        /// <summary>
        /// Adjust scale a bit of wireframe model to prevent z-fighting
        /// </summary>
        [DataMember(10)]
        [DataMemberRange(0.0f, 0.1f, 0.001f, 0.002f, 4)]
        public float ScaleAdjust = 0.001f;

        public override Type SupportedRenderObjectType => typeof(RenderMesh);

        public WireframeRenderFeature()
        {
            SortKey = 255;
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            // initialize shader
            shader = new DynamicEffectInstance("WireframeShader");
            shader.Initialize(Context.Services);

            // create the pipeline state and set properties that won't change
            pipelineState = new MutablePipelineState(Context.GraphicsDevice);
            pipelineState.State.SetDefaults();
            pipelineState.State.InputElements = VertexPositionNormalTexture.Layout.CreateInputElements();
            pipelineState.State.BlendState = BlendStates.AlphaBlend;
            pipelineState.State.RasterizerState.CullMode = CullMode.None;
        }

        public override void Prepare(RenderDrawContext context)
        {
            base.Prepare(context);
        }

        public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage)
        {
            using var _ = Profiler.Begin(DrawKey);
            var commandList = context.CommandList;

            shader.UpdateEffect(context.GraphicsDevice);

            foreach (var renderNode in renderViewStage.SortedRenderNodes)
            {
                var renderMesh = renderNode.RenderObject as RenderMesh;
                if (renderMesh == null)
                {
                    continue;
                }

                // get wireframe script
                WireframeComponent wireframeScript = null;
                if (renderMesh.Source is ModelComponent)
                {
                    wireframeScript = (renderMesh.Source as ModelComponent).Entity.Get<WireframeComponent>();
                }

                if (wireframeScript == null || !wireframeScript.Enabled)
                {
                    continue;
                }

                MeshDraw drawData = renderMesh.ActiveMeshDraw;

                // bind VB
                for (int slot = 0; slot < drawData.VertexBuffers.Length; slot++)
                {
                    var vertexBuffer = drawData.VertexBuffers[slot];
                    commandList.SetVertexBuffer(slot, vertexBuffer.Buffer, vertexBuffer.Offset, vertexBuffer.Stride);
                }

                // set shader parameters
                shader.Parameters.Set(TransformationKeys.WorldViewProjection, renderMesh.World * renderView.ViewProjection); // matrix
                shader.Parameters.Set(TransformationKeys.WorldScale, new Vector3(ScaleAdjust + 1.0f)); // increase size to avoid z-fight
                shader.Parameters.Set(WireframeShaderKeys.Viewport, new Vector4(context.RenderContext.RenderView.ViewSize, 0, 0));
                shader.Parameters.Set(WireframeShaderKeys.LineWidth, wireframeScript.LineWidth);
                shader.Parameters.Set(WireframeShaderKeys.LineColor, wireframeScript.Color);

                // prepare pipeline state
                pipelineState.State.RootSignature = shader.RootSignature;
                pipelineState.State.EffectBytecode = shader.Effect.Bytecode;
                pipelineState.State.PrimitiveType = drawData.PrimitiveType;

                pipelineState.State.Output.CaptureState(commandList);
                pipelineState.Update();

                if (drawData.IndexBuffer != null)
                    commandList.SetIndexBuffer(drawData.IndexBuffer.Buffer, drawData.IndexBuffer.Offset, drawData.IndexBuffer.Is32Bit);

                commandList.SetPipelineState(pipelineState.CurrentState);

                // apply the effect
                shader.Apply(context.GraphicsContext);

                // Draw
                commandList.DrawMesh(drawData, renderMesh.InstanceCount);
            }
        }
    }
}
