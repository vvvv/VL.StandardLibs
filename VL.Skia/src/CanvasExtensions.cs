using SkiaSharp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Stride.Core.Mathematics;

using size_t = System.IntPtr;
using sk_canvas_t = System.IntPtr;
using sk_paint_t = System.IntPtr;

namespace VL.Skia
{
    public static class CanvasExtensions
    {
		private const string SKIA = "libSkiaSharp";

        public static void DrawPoints(this SKCanvas canvas, SKPointMode mode, IEnumerable<Vector2> points, SKPaint paint)
        {
			if (points.TryGetSpan(out var span))
				canvas.DrawPoints(mode, span, paint);
			else
			{
				canvas.DrawPoints(mode, points.ToArray().AsSpan(), paint);
			}
		}

		public static void DrawPoints(this SKCanvas canvas, SKPointMode mode, ReadOnlySpan<Vector2> points, SKPaint paint)
		{
			canvas.DrawPoints(mode, MemoryMarshal.Cast<Vector2, SKPoint>(points), paint);
		}

		[DllImport(SKIA, CallingConvention = CallingConvention.Cdecl)]
		extern static unsafe void sk_canvas_draw_points(sk_canvas_t t, SKPointMode mode, size_t count, SKPoint* pPoints, sk_paint_t paint);

		public static unsafe void DrawPoints(this SKCanvas canvas, SKPointMode mode, ReadOnlySpan<SKPoint> points, SKPaint paint)
		{
			if (canvas is null)
				throw new ArgumentNullException(nameof(canvas));
			if (paint is null)
				throw new ArgumentNullException(nameof(paint));

			fixed (SKPoint* p = points)
			{
				sk_canvas_draw_points(canvas.Handle, mode, (size_t)points.Length, p, paint.Handle);
			}
		}
	}
}
