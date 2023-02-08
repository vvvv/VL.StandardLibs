using System;

namespace VL.Core
{
    public interface IScreenshotService
    {
        void ScreenshotHandle(IntPtr hWnd);
    }
}
