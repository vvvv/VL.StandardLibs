using SkiaSharp;
using System.Buffers;
using System.Runtime.InteropServices;

namespace VL.ImGui
{
    /// <summary>
    /// Draws the triangles of one draw command of ImGui with Skia.
    /// </summary>
    /// <remarks>
    /// ImGui gives a solid shape (a background, a frame, a check mark) the same texture coordinate at every corner: the white pixel of its font atlas.
    /// The GPU interpolates that and draws the colour of the corners. The raster backend of Skia maps the texture coordinates of each triangle onto its
    /// positions, cannot do so when the three are equal, and drops the triangle: without a GPU (a picture recorded for later, a raster surface) every solid
    /// shape of ImGui vanished and only the text was left. There the triangles of the white pixel are drawn without texture coordinates (their vertex colours
    /// alone), the others with the atlas, in the order ImGui gave them.
    /// </remarks>
    static unsafe class ImGuiVertices
    {
        static readonly SKPaint solidPaint = new() { Color = SKColors.White };

        public static void Draw(SKCanvas canvas, SKPoint* positions, SKPoint* texs, SKColor* colors, int vertexCount, ushort* indices, int indexCount, SKPaint paint, SKPoint whitePixel, bool hasGpu)
        {
            if (hasGpu)
            {
                DrawRange(canvas, positions, texs, colors, vertexCount, indices, indexCount, paint);
                return;
            }

            var start = 0;
            var runIsSolid = false;
            for (var t = 0; t < indexCount; t += 3)
            {
                var solid = texs[indices[t]] == whitePixel && texs[indices[t + 1]] == whitePixel && texs[indices[t + 2]] == whitePixel;
                if (t > 0 && solid != runIsSolid)
                {
                    DrawRun(canvas, positions, texs, colors, indices + start, t - start, runIsSolid, paint);
                    start = t;
                }
                runIsSolid = solid;
            }
            if (indexCount > 0)
                DrawRun(canvas, positions, texs, colors, indices + start, indexCount - start, runIsSolid, paint);
        }

        // A run of triangles with the vertices it uses only
        static void DrawRun(SKCanvas canvas, SKPoint* positions, SKPoint* texs, SKColor* colors, ushort* indices, int indexCount, bool solid, SKPaint paint)
        {
            int first = ushort.MaxValue, last = 0;
            for (var i = 0; i < indexCount; i++)
            {
                first = Math.Min(first, indices[i]);
                last = Math.Max(last, indices[i]);
            }
            var rebased = ArrayPool<ushort>.Shared.Rent(indexCount);
            try
            {
                for (var i = 0; i < indexCount; i++)
                    rebased[i] = (ushort)(indices[i] - first);
                fixed (ushort* r = rebased)
                    DrawRange(canvas, positions + first, solid ? null : texs + first, colors + first, last - first + 1, r, indexCount, solid ? solidPaint : paint);
            }
            finally
            {
                ArrayPool<ushort>.Shared.Return(rebased);
            }
        }

        static void DrawRange(SKCanvas canvas, SKPoint* positions, SKPoint* texs, SKColor* colors, int vertexCount, ushort* indices, int indexCount, SKPaint paint)
        {
            // The native call: no allocation per draw command
            var vertices = sk_vertices_make_copy(SKVertexMode.Triangles, vertexCount, positions, texs, (uint*)colors, indexCount, indices);
            sk_canvas_draw_vertices(canvas.Handle, vertices, SKBlendMode.Modulate, paint.Handle);
            sk_vertices_unref(vertices);
        }

        // Taken from SkiaApi - allows us to draw the vertices without allocating
        [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
        static extern void sk_canvas_draw_vertices(IntPtr ccanvas, IntPtr vertices, SKBlendMode mode, IntPtr paint);

        [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr sk_vertices_make_copy(SKVertexMode vmode, int vertexCount, SKPoint* positions, SKPoint* texs, uint* colors, int indexCount, ushort* indices);

        [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
        static extern void sk_vertices_unref(IntPtr cvertices);
    }
}
