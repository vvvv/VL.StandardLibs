using ImGuiNET;
using SkiaSharp;
using System.Buffers;
using System.Runtime.InteropServices;
using VL.ImGui;
using VL.Skia;

namespace VL.Core.ImGui.SkiaBackend
{
    using ImGui = ImGuiNET.ImGui;

    public class SkiaImguiRenderer : IDisposable
    {
        private readonly SkiaContext _context;
        private readonly Handle<SKPaint> _fontPaint;
        private readonly RenderContext _renderContext;

        public SkiaImguiRenderer(SkiaContext context) 
        {
            _context = context;
            _renderContext = RenderContext.ForCurrentThread();
            _fontPaint = new Handle<SKPaint>(new SKPaint());
        }

        public SkiaContext Context => _context;

        public void Dispose()
        {
            _fontPaint.Target?.Dispose();
            _fontPaint.Dispose();
            _renderContext.Dispose();
        }

        public void CreateFontsTexture()
        {
            var paint = _fontPaint.Target;
            if (paint is null)
                throw new ObjectDisposedException(nameof(SkiaImguiRenderer));

            var io = ImGui.GetIO();
            io.Fonts.GetTexDataAsAlpha8(out IntPtr pixels, out var w, out var h);

            if (w == 0)
            {
                // Something went wrong, load default font
                io.Fonts.Clear();
                _context.Fonts.Clear();
                io.Fonts.AddFontDefault();
                io.Fonts.GetTexDataAsAlpha8(out pixels, out w, out h);
            }

            var info = new SKImageInfo(w, h, SKColorType.Alpha8);
            using var pmap = new SKPixmap(info, pixels, info.RowBytes);
            var localMatrix = SKMatrix.CreateScale(1.0f / w, 1.0f / h);
            var fontImage = SKImage.FromPixelCopy(pmap);
            // makeShader(SkSamplingOptions(SkFilterMode::kLinear), localMatrix);
            var fontShader = fontImage.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, localMatrix);
            paint.Shader = fontShader;
            paint.Color = SKColors.White;

            // Store our identifier
            io.Fonts.TexID = _fontPaint.Ptr;
        }

        // From https://github.com/google/skia/blob/main/tools/viewer/ImGuiLayer.cpp
        public void Render(CallerInfo caller, ImDrawDataPtr drawData, bool applyHectoPixelScaling)
        {
            var canvas = caller.Canvas;
            //using var _ = new SKAutoCanvasRestore(canvas, true);
            canvas.Save();
            try
            {
                var us = caller;
                if (applyHectoPixelScaling)
                {
                    us = PushTransformation(caller, SKMatrix.CreateScale(ImGuiConversion.FromImGuiScaling, ImGuiConversion.FromImGuiScaling));
                    canvas.SetMatrix(us.Transformation);
                    //updateScaling(us.Transformation.ScaleY);
                }

                // Our visible imgui space lies from draw_data->DisplayPos (top left) to draw_data->DisplayPos+data_data->DisplaySize (bottom right). DisplayPos is (0,0) for single viewport apps.
                canvas.Translate(-drawData.DisplayPos.X, -drawData.DisplayPos.Y);

                for (int i = 0; i < drawData.CmdListsCount; ++i)
                {
                    var drawList = drawData.CmdListsRange[i];

                    // De-interleave all vertex data (sigh), convert to Skia types
                    //pos.Clear(); uv.Clear(); color.Clear();
                    var size = drawList.VtxBuffer.Size;

                    // Managed approach - allocates
                    //var pos = new SKPoint[size];
                    //var uv = new SKPoint[size];
                    //var color = new SKColor[size];

                    // Native approach - allocation free
                    var pos = ArrayPool<SKPoint>.Shared.Rent(size);
                    var uv = ArrayPool<SKPoint>.Shared.Rent(size);
                    var color = ArrayPool<SKColor>.Shared.Rent(size);

                    for (int j = 0; j < size; ++j)
                    {
                        var vert = drawList.VtxBuffer[j];
                        pos[j] = new SKPoint(vert.pos.X, vert.pos.Y);
                        uv[j] = new SKPoint(vert.uv.X, vert.uv.Y);
                        color[j] = vert.col;
                    }

                    // ImGui colors are RGBA
                    SKSwizzle.SwapRedBlue(MemoryMarshal.AsBytes(color.AsSpan()), MemoryMarshal.AsBytes(color.AsSpan()), size);

                    // Draw everything with canvas.drawVertices...
                    for (int j = 0; j < drawList.CmdBuffer.Size; ++j)
                    {
                        var drawCmd = drawList.CmdBuffer[j];
                        var indexOffset = (int)drawCmd.IdxOffset;
                        var clipRect = new SKRect(drawCmd.ClipRect.X, drawCmd.ClipRect.Y, drawCmd.ClipRect.Z, drawCmd.ClipRect.W);
                        canvas.Save();
                        try
                        {
                            canvas.ClipRect(clipRect);

                            // TODO: Find min/max index for each draw, so we know how many vertices (sigh)
                            if (drawCmd.UserCallback != IntPtr.Zero)
                            {
                                var handle = GCHandle.FromIntPtr(drawCmd.UserCallback);
                                try
                                {
                                    if (handle.Target is DrawCallback callback)
                                        callback(drawList, drawCmd);
                                }
                                finally
                                {
                                    handle.Free();
                                }
                            }
                            else
                            {
                                var idIndex = drawCmd.TextureId.ToInt64();
                                if (0 <= idIndex && idIndex < _context.Layers.Count)
                                {
                                    // Small image IDs are actually indices into a list of callbacks. We directly
                                    // examing the vertex data to deduce the image rectangle, then reconfigure the
                                    // canvas to be clipped and translated so that the callback code gets to use
                                    // Skia to render a widget in the middle of an ImGui panel.
                                    var rectIndex = drawList.IdxBuffer[indexOffset];
                                    var tl = pos[rectIndex];
                                    var br = pos[rectIndex + 2];
                                    var imageClipRect = new SKRect(tl.X, tl.Y, br.X, br.Y);

                                    canvas.SetMatrix(caller.Transformation);
                                    try
                                    {
                                        _context.Layers[(int)idIndex].Render(caller);
                                    }
                                    finally
                                    {
                                        canvas.SetMatrix(us.Transformation);
                                    }
                                }
                                else if (drawCmd.ElemCount > 0)
                                {
                                    var handle = GCHandle.FromIntPtr(drawCmd.TextureId);
                                    var paint = handle.Target as SKPaint ?? _fontPaint.Target;
                                    if (paint is null)
                                        continue;

                                    // Managed approach - we need to allocate arrays for each call
                                    //var indices = new ushort[drawCmd.ElemCount];
                                    //for (int k = 0; k < indices.Length; k++)
                                    //    indices[k] = drawList.IdxBuffer[indexOffset + k];

                                    //canvas.DrawVertices(SKVertexMode.Triangles, pos, uv, color, SKBlendMode.Modulate, indices, paint);

                                    // Native approach - allocation free
                                    unsafe
                                    {
                                        var indexPtr = (ushort*)drawList.IdxBuffer.Data.ToPointer() + drawCmd.IdxOffset;
                                        fixed (SKPoint* pPos = pos)
                                        fixed (SKPoint* pTex = uv)
                                        fixed (SKColor* pColor = color)
                                        {
                                            var vertices = sk_vertices_make_copy(SKVertexMode.Triangles, size, pPos, pTex, (uint*)pColor, (int)drawCmd.ElemCount, indexPtr);
                                            sk_canvas_draw_vertices(canvas.Handle, vertices, SKBlendMode.Modulate, paint.Handle);
                                            sk_vertices_unref(vertices);

                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            canvas.Restore();
                        }
                    }

                    ArrayPool<SKPoint>.Shared.Return(pos);
                    ArrayPool<SKPoint>.Shared.Return(uv);
                    ArrayPool<SKColor>.Shared.Return(color);
                }
            }
            finally
            {
                canvas.Restore();
            }

            CallerInfo PushTransformation(CallerInfo caller, SKMatrix relative)
            {
                SKMatrix target = caller.Transformation;
#pragma warning disable CS0618 // Type or member is obsolete
                SKMatrix.PreConcat(ref target, ref relative);
#pragma warning restore CS0618 // Type or member is obsolete
                return caller.WithTransformation(target);
            }

            // Taken from SkiaApi - allows us to draw the vertices without allocating
            [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
            static extern void sk_canvas_draw_vertices(IntPtr ccanvas, IntPtr vertices, SKBlendMode mode, IntPtr paint);

            [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
            unsafe static extern IntPtr sk_vertices_make_copy(SKVertexMode vmode, int vertexCount, SKPoint* positions, SKPoint* texs, uint* colors, int indexCount, ushort* indices);

            [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
            static extern void sk_vertices_unref(IntPtr cvertices);
        }
    }
}
