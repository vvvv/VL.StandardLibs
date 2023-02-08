using System;
using System.Drawing;
using System.Windows.Forms;
using VL.Core;
using Clipboard = System.Windows.Forms.Clipboard;

namespace VL.CoreLib.Windows
{
    public class ScreenshotHelper : IScreenshotService
    {
        public void ScreenshotHandle(IntPtr hWnd)
        {
            User32.GetClientRect(hWnd, out var canvas);

            using (var bitmap = new Bitmap(canvas.Width, canvas.Height))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    var src = User32.GetWindowDC(hWnd);
                    var dest = g.GetHdc();
                    var x = SystemInformation.FrameBorderSize.Width * 2;
                    var y = SystemInformation.FrameBorderSize.Height * 2 + SystemInformation.CaptionHeight;
                    Gdi32.StretchBlt(dest, 0, 0, canvas.Width, canvas.Height, src, x, y, canvas.Width, canvas.Height, TernaryRasterOperations.SRCCOPY);
                    g.ReleaseHdc();

                    Clipboard.SetImage(bitmap);
                }
            }
        }
    }
}
