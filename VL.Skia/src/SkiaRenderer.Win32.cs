#nullable enable

extern alias sw;

using SkiaSharp;
using System;
using System.Windows.Forms;
using sw::VL.Skia;
using RGBA = Stride.Core.Mathematics.Color4;
using sw::VL.Core.Windows;

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
        {
            if (!customTitleBar.ShouldDrawTitleBarButtons)
                return;

            int iconDimension = LogicalToDeviceUnits(10);

            // Icon color
            var iconColor = SKColors.White;
            // Use dark gray for idle and a brighter gray for hover so it is visible on both dark and light backgrounds.
            var idleBgColor = new SKColor(60, 60, 60, 90);    // subtle dark gray
            var hoverBgColor = new SKColor(170, 170, 170, 140); // brighter gray (lightens on hover)

            using var iconStrokePaint = new SKPaint
            {
                Color = iconColor,
                IsStroke = true,
                StrokeWidth = 1f,
                IsAntialias = false,
                StrokeCap = SKStrokeCap.Square
            };
            using var iconFillPaint = new SKPaint
            {
                Color = iconColor,
                IsStroke = false,
                IsAntialias = false
            };

            // Helper local scope (no function) to draw each button without creating delegates/local function frames.
            // Minimize button background
            var e = customTitleBar;
            using (var bgPaint = new SKPaint { Color = e.MinimizeButtonHovered ? hoverBgColor : idleBgColor, IsStroke = false, IsAntialias = false })
                canvas.DrawRect(e.MinimizeButtonRect.Left, e.MinimizeButtonRect.Top, e.MinimizeButtonRect.Width, e.MinimizeButtonRect.Height, bgPaint);
            // Minimize icon (single horizontal line)
            var minY = e.MinimizeButtonRect.Y + (e.MinimizeButtonRect.Height - 1) / 2f;
            var minX = e.MinimizeButtonRect.X + (e.MinimizeButtonRect.Width - iconDimension) / 2f;
            canvas.DrawRect(new SKRect(minX, minY, minX + iconDimension, minY + 1), iconFillPaint);

            // Maximize button background
            using (var bgPaint = new SKPaint { Color = e.MaximizeButtonHovered ? hoverBgColor : idleBgColor, IsStroke = false, IsAntialias = false })
                canvas.DrawRect(e.MaximizeButtonRect.Left, e.MaximizeButtonRect.Top, e.MaximizeButtonRect.Width, e.MaximizeButtonRect.Height, bgPaint);
            // Maximize icon
            var maxLeft = e.MaximizeButtonRect.X + (e.MaximizeButtonRect.Width - iconDimension) / 2f;
            var maxTop = e.MaximizeButtonRect.Y + (e.MaximizeButtonRect.Height - iconDimension) / 2f;
            var maxRect = new SKRect(maxLeft, maxTop, maxLeft + iconDimension, maxTop + iconDimension);
            if (e.IsMaximized)
            {
                var offset = LogicalToDeviceUnits(2);
                var backRect = new SKRect(maxRect.Left + offset, maxRect.Top - offset, maxRect.Right + offset, maxRect.Bottom - offset);
                canvas.DrawRect(backRect, iconStrokePaint);
            }
            canvas.DrawRect(maxRect, iconStrokePaint);

            // Close button background
            using (var bgPaint = new SKPaint { Color = e.CloseButtonHovered ? hoverBgColor : idleBgColor, IsStroke = false, IsAntialias = false })
                canvas.DrawRect(e.CloseButtonRect.Left, e.CloseButtonRect.Top, e.CloseButtonRect.Width, e.CloseButtonRect.Height, bgPaint);
            // Close icon (X)
            var closeLeft = e.CloseButtonRect.X + (e.CloseButtonRect.Width - iconDimension) / 2f;
            var closeTop = e.CloseButtonRect.Y + (e.CloseButtonRect.Height - iconDimension) / 2f;
            var closeRight = closeLeft + iconDimension;
            var closeBottom = closeTop + iconDimension;
            canvas.DrawLine(closeLeft, closeTop, closeRight, closeBottom, iconStrokePaint);
            canvas.DrawLine(closeLeft, closeBottom, closeRight, closeTop, iconStrokePaint);
        }
    }
}
