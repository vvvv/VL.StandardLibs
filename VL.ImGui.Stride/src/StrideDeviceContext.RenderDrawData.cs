using ImGuiNET;
using Stride.Graphics;
using Stride.Core.Mathematics;
using Buffer = Stride.Graphics.Buffer;
using Stride.Rendering;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VL.ImGui.Stride.Effects;
using VL.Stride.Input;

namespace VL.ImGui
{
    partial class StrideDeviceContext
    {
        private Vector2 offset = Vector2.Zero;

        public void SetOffset(Vector2 offset)
        {
            this.offset = offset;
        }

        void CheckBuffers(GraphicsDevice device, ImDrawDataPtr drawData)
        {
            uint totalVBOSize = (uint)(drawData.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
            if (totalVBOSize > vertexBinding.Buffer.SizeInBytes)
            {
                var vertexBuffer = Buffer.Vertex.New(device, (int)(totalVBOSize * 1.5f));
                vertexBinding = new VertexBufferBinding(vertexBuffer, imVertLayout, 0);
            }

            uint totalIBOSize = (uint)(drawData.TotalIdxCount * sizeof(ushort));
            if (totalIBOSize > indexBinding?.Buffer.SizeInBytes)
            {
                var is32Bits = false;
                var indexBuffer = Buffer.Index.New(device, (int)(totalIBOSize * 1.5f));
                indexBinding = new IndexBufferBinding(indexBuffer, is32Bits, 0);
            }
        }

        void UpdateBuffers(CommandList commandList, ImDrawDataPtr drawData)
        {
            // copy de dators
            int vtxOffsetBytes = 0;
            int idxOffsetBytes = 0;

            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                ImDrawListPtr cmdList = drawData.CmdLists[n];
                vertexBinding.Buffer.SetData(commandList, new DataPointer(cmdList.VtxBuffer.Data, cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()), vtxOffsetBytes);
                indexBinding.Buffer.SetData(commandList, new DataPointer(cmdList.IdxBuffer.Data, cmdList.IdxBuffer.Size * sizeof(ushort)), idxOffsetBytes);
                vtxOffsetBytes += cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
                idxOffsetBytes += cmdList.IdxBuffer.Size * sizeof(ushort);
            }
        }

        public void RenderDrawLists(RenderDrawContext context, ImDrawDataPtr drawData)
        {

            var inputSource = context.RenderContext.GetWindowInputSource();
            var commandList = context.CommandList;      
            var projMatrix = Matrix.OrthoLH(drawData.DisplaySize.X, -drawData.DisplaySize.Y, -1, 1);
            Vector2 off = new Vector2(drawData.DisplayPos.X, drawData.DisplayPos.Y);

            CheckBuffers(context.GraphicsDevice, drawData); // potentially resize buffers first if needed
            UpdateBuffers(commandList, drawData); // updeet em now

            int vtxOffset = 0;
            int idxOffset = 0;

            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                ImDrawListPtr cmdList = drawData.CmdLists[n];

                for (int i = 0; i < cmdList.CmdBuffer.Size; i++)
                {
                    ImDrawCmdPtr cmd = cmdList.CmdBuffer[i];

                    if (cmd.UserCallback != IntPtr.Zero)
                    {
                        RenderLayerWithViewPort? renderLayer = this.GetRenderer(cmd.UserCallback);

                        if (renderLayer != null)
                        {
                            renderLayer.ParentInputSource = renderLayer.HasFocus ? inputSource : null;

                            if (renderLayer.Viewport.Size.LengthSquared() > 0 && context != null)
                            {
                                var renderContext = context.RenderContext;

                                using (renderContext.SaveRenderOutputAndRestore())
                                using (renderContext.SaveViewportAndRestore())
                                {
                                    var layer = renderLayer.Layer;
                                    if (layer != null)
                                    {
                                        if (layer is ImGuiRenderer renderer)
                                        {
                                            renderer.ErrorImGuiInsideImGui();
                                        }
                                        else
                                        {
                                            if (layer is SkiaRendererWithOffset skiaWithOffset)
                                            {
                                                skiaWithOffset.SetOffset(off);
                                            }
                                           
                                            renderLayer.Offset = off;
                                            context?.CommandList.SetViewport((Viewport)renderLayer.Viewport);
                                            renderLayer.Layer?.Draw(context);
                                            context?.CommandList.SetViewport(renderContext.ViewportState.Viewport0);
                                           
                                        }

                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Texture? tex = null;

                        if (cmd.TextureId != IntPtr.Zero)
                        {
                            var textureHandle = GCHandle.FromIntPtr(cmd.TextureId);
                            if (textureHandle.Target is Texture texture)
                            {
                                tex = texture;
                            }
                        }
                        else
                        {
                            tex = fontTexture;
                        }
                        if (tex != null)
                        {
                            var is32Bits = false;
                            commandList.SetPipelineState(imPipeline);
                            commandList.SetVertexBuffer(0, vertexBinding.Buffer, 0, Unsafe.SizeOf<ImDrawVert>());
                            commandList.SetIndexBuffer(indexBinding.Buffer, 0, is32Bits);

                            commandList.SetScissorRectangle(
                                new Rectangle(
                                    (int)cmd.ClipRect.X - (int)drawData.DisplayPos.X,
                                    (int)cmd.ClipRect.Y - (int)drawData.DisplayPos.Y,
                                    (int)(cmd.ClipRect.Z - cmd.ClipRect.X),
                                    (int)(cmd.ClipRect.W - cmd.ClipRect.Y)
                                )
                            );

                            //imShader.SetParameters(context?.RenderContext.RenderView, context);
                            imShader.Parameters.Set(TexturingKeys.Texture0, tex);
                            imShader.Parameters.Set(ImGuiEffectShaderKeys.proj, ref projMatrix);
                            imShader.Parameters.Set(ImGuiEffectShaderKeys.offset, ref off);
                            
                            imShader.Apply(graphicsContext);

                            commandList.DrawIndexed((int)cmd.ElemCount, idxOffset, vtxOffset);
                        }

                        idxOffset += (int)cmd.ElemCount;
                    }
                }
                vtxOffset += cmdList.VtxBuffer.Size;
            }
        }
    }
}
