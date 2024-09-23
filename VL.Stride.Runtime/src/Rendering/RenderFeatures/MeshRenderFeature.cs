// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading;

using Stride.Core.Mathematics;
using Stride.Core.Diagnostics;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

using VL.Stride.Graphics;

namespace VL.Stride.Rendering
{
    /// <summary>
    /// Renders <see cref="RenderMesh"/>.
    /// </summary>
    public class MeshRenderFeature : global::Stride.Rendering.MeshRenderFeature
    {
        private readonly ThreadLocal<DescriptorSet[]> descriptorSets = new ThreadLocal<DescriptorSet[]>();
        private static readonly ProfilingKey DrawKey = new ProfilingKey("MeshRenderFeatureExt.Draw");
        private Buffer emptyBuffer;

        protected override void InitializeCore()
        {
            base.InitializeCore();

            // Create an empty buffer to compensate for missing vertex streams
            emptyBuffer = Buffer.Vertex.New(Context.GraphicsDevice, new Vector4[1]);
        }

        protected override void Destroy()
        {
            descriptorSets.Dispose();

            emptyBuffer?.Dispose();
            emptyBuffer = null;

            base.Destroy();
        }

        /// <inheritdoc/>
        public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
        {
            using var _ = Profiler.Begin(DrawKey);
            var commandList = context.CommandList;

            foreach (var renderFeature in RenderFeatures)
            {
                renderFeature.Draw(context, renderView, renderViewStage, startIndex, endIndex);
            }

            // TODO: stackalloc?
            var descriptorSetsLocal = descriptorSets.Value;
            if (descriptorSetsLocal == null || descriptorSetsLocal.Length < EffectDescriptorSetSlotCount)
            {
                descriptorSetsLocal = descriptorSets.Value = new DescriptorSet[EffectDescriptorSetSlotCount];
            }

            MeshDraw currentDrawData = null;
            int emptyBufferSlot = -1;
            for (int index = startIndex; index < endIndex; index++)
            {
                var renderNodeReference = renderViewStage.SortedRenderNodes[index].RenderNode;
                var renderNode = GetRenderNode(renderNodeReference);

                var renderMesh = (RenderMesh)renderNode.RenderObject;
                var drawData = renderMesh.ActiveMeshDraw;

                // Get effect
                // TODO: Use real effect slot
                var renderEffect = renderNode.RenderEffect;
                if (renderEffect.Effect == null)
                    continue;

                // Bind VB
                if (currentDrawData != drawData)
                {
                    for (int slot = 0; slot < drawData.VertexBuffers.Length; slot++)
                    {
                        var vertexBuffer = drawData.VertexBuffers[slot];
                        commandList.SetVertexBuffer(slot, vertexBuffer.Buffer, vertexBuffer.Offset, vertexBuffer.Stride);
                    }

                    // If the mesh's vertex buffers miss any input streams, an additional input binding will have been added to the pipeline state.
                    // We bind an additional empty vertex buffer to that slot handle those streams gracefully.
                    if (emptyBufferSlot != drawData.VertexBuffers.Length)
                    {
                        commandList.SetVertexBuffer(drawData.VertexBuffers.Length, emptyBuffer, 0, 0);
                        emptyBufferSlot = drawData.VertexBuffers.Length;
                    }

                    if (drawData.IndexBuffer != null)
                        commandList.SetIndexBuffer(drawData.IndexBuffer.Buffer, drawData.IndexBuffer.Offset, drawData.IndexBuffer.Is32Bit);
                    currentDrawData = drawData;
                }

                var resourceGroupOffset = ComputeResourceGroupOffset(renderNodeReference);

                // Update cbuffer
                renderEffect.Reflection.BufferUploader.Apply(context.CommandList, ResourceGroupPool, resourceGroupOffset);

                // Bind descriptor sets
                for (int i = 0; i < descriptorSetsLocal.Length; ++i)
                {
                    var resourceGroup = ResourceGroupPool[resourceGroupOffset++];
                    if (resourceGroup != null)
                        descriptorSetsLocal[i] = resourceGroup.DescriptorSet;
                }

                commandList.SetPipelineState(renderEffect.PipelineState);
                commandList.SetDescriptorSets(0, descriptorSetsLocal);

                // Draw
                if (drawData.IndexBuffer == null)
                {
                    if (renderMesh.InstanceCount > 0)
                        commandList.DrawInstanced(drawData.DrawCount, renderMesh.InstanceCount, drawData.StartLocation);
                    else
                        commandList.Draw(drawData.DrawCount, drawData.StartLocation);
                }
                else
                {
                    if (drawData.IndexBuffer is IndirectIndexBufferBinding)
                    {
                        var indexbuffer = drawData.IndexBuffer as IndirectIndexBufferBinding;

                        if (renderMesh.InstanceCount > 0)
                            commandList.DrawIndexedInstanced(indexbuffer.DrawArgs);
                        else
                            commandList.DrawInstanced(indexbuffer.DrawArgs);
                    }
                    else
                    {
                        if (renderMesh.InstanceCount > 0)
                            commandList.DrawIndexedInstanced(drawData.DrawCount, renderMesh.InstanceCount, drawData.StartLocation);
                        else
                            commandList.DrawIndexed(drawData.DrawCount, drawData.StartLocation);
                    }
                }
            }
        }
    }
}