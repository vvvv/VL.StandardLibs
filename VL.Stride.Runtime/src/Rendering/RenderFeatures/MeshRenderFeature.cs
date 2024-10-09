// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading;

using Stride.Core.Mathematics;
using Stride.Core.Diagnostics;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;
using System.Reflection;
using System;
using System.Linq;
using MeshDrawStride = global::Stride.Rendering.MeshDraw;
using MeshDrawIndirect = VL.Stride.Rendering.MeshDraw;

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

            MeshDrawStride currentDrawData = null;
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
                if (drawData is MeshDrawIndirect && ((MeshDrawIndirect)drawData).DrawArgs != null)
                {
                    var drawDataIndirect = (MeshDrawIndirect)drawData;
                    if (drawData.IndexBuffer == null)
                    {
                        // TODO PR for Stride to fix commandList.DrawInstanced
                        // https://github.com/stride3d/stride/pull/2482
                        CommandListHelper.DrawInstanced(commandList, drawDataIndirect.DrawArgs, 0);
                    }
                    else
                    {
                        commandList.DrawIndexedInstanced(drawDataIndirect.DrawArgs, 0);
                    }
                }
                else
                { 
                    if (drawData.IndexBuffer == null)
                    {
                        if (renderMesh.InstanceCount > 0)
                            commandList.DrawInstanced(drawData.DrawCount, renderMesh.InstanceCount, drawData.StartLocation);
                        else
                            commandList.Draw(drawData.DrawCount, drawData.StartLocation);
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

    public static class CommandListHelper
    {
        static FieldInfo nativeDeviceContextFi;
        static FieldInfo nativeDeviceChildFi;

        static MethodInfo PrepareDraw;
        static object[] arg;


        static CommandListHelper()
        {
            PrepareDraw = typeof(CommandList).GetMethod("PrepareDraw", BindingFlags.Instance | BindingFlags.NonPublic);
            nativeDeviceContextFi = typeof(CommandList).GetRuntimeFields().Where(fi => fi.Name == "nativeDeviceContext").First();

            var graphicsResourceBaseType = Type.GetType("Stride.Graphics.GraphicsResourceBase, Stride.Graphics");
            nativeDeviceChildFi = graphicsResourceBaseType.GetRuntimeFields().Where(i => i.Name == "nativeDeviceChild").First();
        }

        public static SharpDX.Direct3D11.DeviceContext GetNativeDeviceContext(this CommandList commandList)
        {
            return (SharpDX.Direct3D11.DeviceContext)nativeDeviceContextFi.GetValue(commandList);
        }

        public static void DrawInstanced(this CommandList commandList, Buffer argumentsBuffer, int alignedByteOffsetForArgs = 0)
        {
            if (argumentsBuffer == null) throw new ArgumentNullException("argumentsBuffer");

            PrepareDraw.Invoke(commandList, arg);

            var buffer = (SharpDX.Direct3D11.Buffer)nativeDeviceChildFi.GetValue(argumentsBuffer);

            commandList.GetNativeDeviceContext().DrawInstancedIndirect(buffer, alignedByteOffsetForArgs);

            commandList.GraphicsDevice.FrameDrawCalls++;
        }
    }
}