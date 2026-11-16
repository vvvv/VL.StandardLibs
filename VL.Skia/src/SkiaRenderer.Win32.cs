#nullable enable
#if WINDOWS

extern alias sw;

using SkiaSharp;
using System;
using System.Windows.Forms;
using sw::System.Windows.Forms;
using sw::VL.Skia;
using VL.Core.Windows;

namespace VL.Skia
{
    partial class SkiaRenderer
    {
        private readonly Win32CustomTitleBar customTitleBar;

        protected override void WndProc(ref Message m)
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(8) && TouchMessageProcessor.TryHandle(ref m, this, touchNotifications))
                return;

            // Let the custom title bar handle Win32 messages
            if (customTitleBar.ProcessMessage(ref m) == true)
                return;

            base.WndProc(ref m);
        }

        public void PaintTitleBarButtons(SKCanvas canvas)
            => TitleBarButtons.Paint(canvas, customTitleBar, WindowState == FormWindowState.Maximized, this.LogicalToDeviceScalingFactor());
    }
}
#endif