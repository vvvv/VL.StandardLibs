using System;
using System.Drawing;
using Vector2 = Stride.Core.Mathematics.Vector2;
using Size2F = Stride.Core.Mathematics.Size2F;

namespace VL.UI.Core
{
    public sealed class DIPHelpers
    {
        static public int DIPToPixel(int dip)
        {
            return (int)(dip * DIPFactor());
        }

        static public float DIPToPixel(float dip)
        {
            return (int)(dip * DIPFactor());
        }

        static public double DIPToPixel(double dip)
        {
            return (int)(dip * DIPFactor());
        }

        static public Point DIPToPixel(Point dip)
        {
            return new Point(DIPToPixel(dip.X), DIPToPixel(dip.Y));
        }

        static public PointF DIPToPixel(PointF dip)
        {
            return new PointF(DIPToPixel(dip.X), DIPToPixel(dip.Y));
        }

        public static Vector2 DIPToPixel(Vector2 dip)
        {
            return new Vector2(DIPToPixel(dip.X), DIPToPixel(dip.Y));
        }

        public static Size2F DIPToPixel(Size2F pixel)
        {
            return new Size2F(DIPToPixel(pixel.Width), DIPToPixel(pixel.Height));
        }

        static public Size DIPToPixel(Size pixel)
        {
            return new Size(DIPToPixel(pixel.Width), DIPToPixel(pixel.Height));
        }

        static public SizeF DIPToPixel(SizeF dip)
        {
            return new SizeF(DIPToPixel(dip.Width), DIPToPixel(dip.Height));
        }

        static public Rectangle DIPToPixel(Rectangle pixel)
        {
            return new Rectangle(DIPToPixel(pixel.Location), DIPToPixel(pixel.Size));
        }

        static public RectangleF DIPToPixel(RectangleF pixel)
        {
            return new RectangleF(DIPToPixel(pixel.Location), DIPToPixel(pixel.Size));
        }

        static public Stride.Core.Mathematics.RectangleF DIPToPixel(Stride.Core.Mathematics.RectangleF pixel)
        {
            return new Stride.Core.Mathematics.RectangleF(DIPToPixel(pixel.X), DIPToPixel(pixel.Y), DIPToPixel(pixel.Width), DIPToPixel(pixel.Height));
        }

        static public int DIP(int pixel)
        {
            return (int) (pixel / DIPFactor());
        }
        
        static public float DIP(float pixel)
        {
            return pixel / DIPFactor();
        }
        
        static public double DIP(double pixel)
        {
            return pixel / DIPFactor();
        }
        
        static public Point DIP(Point pixel)
        {
            return new Point((int) (pixel.X / DIPFactor()), (int) (pixel.Y / DIPFactor()));
        }
        
        static public PointF DIP(PointF pixel)
        {
            return new PointF(pixel.X / DIPFactor(), pixel.Y / DIPFactor());
        }

        static public Vector2 DIP(Vector2 pixel)
        {
            return new Vector2(pixel.X / DIPFactor(), pixel.Y / DIPFactor());
        }

        static public Size DIP(Size pixel)
        {
            return new Size(DIP(pixel.Width), DIP(pixel.Height));
        }

        static public SizeF DIP(SizeF pixel)
        {
            return new SizeF(DIP(pixel.Width), DIP(pixel.Height));
        }

        static public Rectangle DIP(Rectangle pixel)
        {
            return new Rectangle(DIP(pixel.Location), DIP(pixel.Size));
        }

        static public RectangleF DIP(RectangleF pixel)
        {
            return new RectangleF(DIP(pixel.Location), DIP(pixel.Size));
        }

        static public Stride.Core.Mathematics.RectangleF DIP(Stride.Core.Mathematics.RectangleF inPixels)
        {
            return new Stride.Core.Mathematics.RectangleF(DIP(inPixels.X), DIP(inPixels.Y), DIP(inPixels.Width), DIP(inPixels.Height));
        }

        static float FDIPFactor = -1;
        static public float DIPFactor()
        {
            if (FDIPFactor == -1)
            {
                if (OperatingSystem.IsWindows())
                {
                    using (var g = Graphics.FromHwnd(IntPtr.Zero))
                    {
                        FDIPFactor = g.DpiX / 96;
                    }
                }
                else
                {
                    FDIPFactor = 1;
                }
            }

            return FDIPFactor;
        }
    }
}
