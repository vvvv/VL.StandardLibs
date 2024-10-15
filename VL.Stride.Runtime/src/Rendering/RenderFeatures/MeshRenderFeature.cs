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
using System.Runtime.CompilerServices;
using VL.Lib.Reactive;

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

                commandList.DrawMesh(drawData, renderMesh.InstanceCount);

                //// Draw
                //if (drawData is MeshDrawIndirect drawDataIndirect && drawDataIndirect.DrawArgs != null)
                //{
                //    if (drawData.IndexBuffer == null)
                //    {
                //        if ((drawData.VertexBuffers[0].Buffer.Flags & BufferFlags.StreamOutput) != 0)
                //            commandList.DrawAuto();
                //        else
                //        {
                //            // TODO PR for Stride to fix commandList.DrawInstanced
                //            // https://github.com/stride3d/stride/pull/2482
                //            CommandListHelper.DrawInstanced(commandList, drawDataIndirect.DrawArgs, 0);
                //        }
                //    }
                //    else
                //    {
                //        commandList.DrawIndexedInstanced(drawDataIndirect.DrawArgs, 0);
                //    }
                //}
                //else
                //{ 
                //    if (drawData.IndexBuffer == null)
                //    {
                //        if(drawData.VertexBuffers[0].Buffer.Flags == BufferFlags.StreamOutput)
                //            commandList.DrawAuto();
                //        else if (renderMesh.InstanceCount > 0)
                //            commandList.DrawInstanced(drawData.DrawCount, renderMesh.InstanceCount, drawData.StartLocation);
                //        else
                //            commandList.Draw(drawData.DrawCount, drawData.StartLocation);
                //    }
                //    else
                //    {
                //        if (renderMesh.InstanceCount > 0)
                //            commandList.DrawIndexedInstanced(drawData.DrawCount, renderMesh.InstanceCount, drawData.StartLocation);
                //        else
                //            commandList.DrawIndexed(drawData.DrawCount, drawData.StartLocation);
                //    }
                //}
            }
        }
    }

    public static class CommandListHelper
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(PrepareDraw))]
        extern static void PrepareDraw(this CommandList c);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "nativeDeviceContext")]
        extern static ref SharpDX.Direct3D11.DeviceContext NativeDeviceContext(this CommandList commandList);

        static SharpDX.Direct3D11.Buffer NativeBuffer(this Buffer buffer)
        {
            ref var buf = ref NativeBuffer(buffer);

            return (SharpDX.Direct3D11.Buffer)buf;

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "nativeDeviceChild")]
            extern static ref SharpDX.Direct3D11.DeviceChild NativeBuffer(GraphicsResourceBase buffer);
        }

        static CommandList DrawInstanced(this CommandList commandList, Buffer argumentsBuffer, int alignedByteOffsetForArgs = 0)
        {
            if (argumentsBuffer == null) throw new ArgumentNullException("argumentsBuffer");

            commandList.PrepareDraw();  
            commandList.NativeDeviceContext().DrawInstancedIndirect(argumentsBuffer.NativeBuffer(), alignedByteOffsetForArgs);
            commandList.GraphicsDevice.FrameDrawCalls++;

            return commandList;
        }

        public static CommandList DrawMesh(this CommandList commandList, MeshDraw drawData, int InstanceCount)
        {
            // Draw
            if (drawData is MeshDrawIndirect drawDataIndirect && drawDataIndirect.DrawArgs != null)
            {
                if (drawData.IndexBuffer == null)
                {
                    if ((drawData.VertexBuffers[0].Buffer.Flags & BufferFlags.StreamOutput) != 0 && drawDataIndirect.DrawAuto)
                        commandList.DrawAuto();
                    else
                        // TODO PR for Stride to fix commandList.DrawInstanced
                        // https://github.com/stride3d/stride/pull/2482
                        DrawInstanced(commandList, drawDataIndirect.DrawArgs, 0); 
                }
                else
                    commandList.DrawIndexedInstanced(drawDataIndirect.DrawArgs, 0);
            }
            else
            {
                if (drawData.IndexBuffer == null)
                {
                    if (InstanceCount > 0)
                        commandList.DrawInstanced(drawData.DrawCount, InstanceCount, drawData.StartLocation);
                    else
                        commandList.Draw(drawData.DrawCount, drawData.StartLocation);
                }
                else
                {
                    if (InstanceCount > 0)
                        commandList.DrawIndexedInstanced(drawData.DrawCount, InstanceCount, drawData.StartLocation);
                    else
                        commandList.DrawIndexed(drawData.DrawCount, drawData.StartLocation);
                }
            }

            return commandList;
        }
    }
}