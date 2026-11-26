using NUnit.Framework;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using VL.Lib.Basics.Imaging;

namespace VL.Core.Tests
{
    [TestFixture]
    [SupportedOSPlatform("windows")]
    public class BitmapImageTests
    {
        [Test]
        public void ABitmapStoredBottomUp_GivesItsRowsTopFirst()
        {
            using var source = new Bitmap(3, 2);
            source.SetPixel(0, 0, Color.FromArgb(200, 30, 10));
            source.SetPixel(2, 1, Color.FromArgb(10, 30, 200));
            var hbitmap = source.GetHbitmap();
            try
            {
                // A bitmap made of an HBITMAP stores its rows bottom up; the clipboard of Windows Forms gives such a bitmap
                using var bitmap = Image.FromHbitmap(hbitmap);
                using var image = bitmap.ToImage(takeOwnership: false);
                using var data = image.GetData();
                Assert.That(data.ScanSize, Is.GreaterThan(0));

                // B, G, R of the first pixel of the first row, and of the last pixel of the last row
                var bytes = data.Bytes.Span;
                Assert.That((bytes[2], bytes[1], bytes[0]), Is.EqualTo(((byte)200, (byte)30, (byte)10)));
                var last = data.ScanSize + 2 * 4;
                Assert.That((bytes[last + 2], bytes[last + 1], bytes[last]), Is.EqualTo(((byte)10, (byte)30, (byte)200)));
            }
            finally { DeleteObject(hbitmap); }
        }

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr obj);
    }
}
