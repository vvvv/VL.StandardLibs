#nullable enable

using System;
using System.Windows.Forms;
using SkiaSharp;
using System.Drawing.Imaging;
using System.Drawing;
using Rectangle = System.Drawing.Rectangle;

namespace VL.Skia
{
    public class SkiaControl : SkiaControlBase
    {
        private Bitmap? bitmap;

        public SkiaControl()
        {
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);

            FreeBitmap();
        }

        protected override sealed void OnPaintCore(PaintEventArgs e)
        {
            // get the bitmap
            var info = CreateBitmap();

            if (bitmap is null)
                return;

            var data = bitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

            // create the surface
            using (var surface = SKSurface.Create(info, data.Scan0, data.Stride))
            {
                var canvas = surface.Canvas;
                CallerInfo = CallerInfo.InRenderer(info.Width, info.Height, canvas, null);
                // start drawing
                using (new SKAutoCanvasRestore(canvas, true))
                {
                    OnPaint(CallerInfo);
                }
                canvas.Flush();
            }

            // write the bitmap to the graphics
            bitmap.UnlockBits(data);
            e.Graphics.DrawImage(bitmap, 0, 0);
        }

        private SKImageInfo CreateBitmap()
        {
            var info = new SKImageInfo(Width, Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

            if (bitmap == null || bitmap.Width != info.Width || bitmap.Height != info.Height)
            {
                FreeBitmap();

                if (info.Width != 0 && info.Height != 0)
                    bitmap = new Bitmap(info.Width, info.Height, PixelFormat.Format32bppPArgb);
            }

            return info;
        }

        private void FreeBitmap()
        {
            if (bitmap != null)
            {
                bitmap.Dispose();
                bitmap = null;
            }
        }
    }
}