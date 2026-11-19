#nullable enable
using SkiaSharp;
using System;
using VL.Core.Windowing;

namespace VL.Skia;

/// <summary>Draws the caption buttons of a title bar that the content covers (<see cref="IWindowTitleBar"/>): minimize, maximize or restore, close.</summary>
public static class TitleBarButtons
{
    // Dark gray at rest and a brighter gray under the mouse, visible on dark and on light content
    static readonly SKColor IdleBackground = new(60, 60, 60, 90);
    static readonly SKColor HoverBackground = new(170, 170, 170, 140);

    /// <summary>Draws the buttons if the title bar says they are needed now (<see cref="IWindowTitleBar.ShouldDrawButtons"/>).</summary>
    /// <param name="canvas">The canvas of the client area, in pixels.</param>
    /// <param name="titleBar">Where the buttons are and which one is under the mouse.</param>
    /// <param name="isMaximized">Draws the restore symbol instead of the maximize symbol.</param>
    /// <param name="scaling">Pixels per DIP of the window.</param>
    public static void Paint(SKCanvas canvas, IWindowTitleBar titleBar, bool isMaximized, float scaling)
    {
        if (!titleBar.ShouldDrawButtons)
            return;

        int iconDimension = ToPixels(10, scaling);

        using var iconStrokePaint = new SKPaint
        {
            Color = SKColors.White,
            IsStroke = true,
            StrokeWidth = 1f,
            IsAntialias = false,
            StrokeCap = SKStrokeCap.Square
        };
        using var iconFillPaint = new SKPaint
        {
            Color = SKColors.White,
            IsStroke = false,
            IsAntialias = false
        };

        // Minimize: a horizontal line
        var minimize = titleBar.MinimizeButton.Bounds;
        PaintBackground(canvas, titleBar.MinimizeButton);
        var minY = minimize.Y + (minimize.Height - 1) / 2f;
        var minX = minimize.X + (minimize.Width - iconDimension) / 2f;
        canvas.DrawRect(new SKRect(minX, minY, minX + iconDimension, minY + 1), iconFillPaint);

        // Maximize: a square, restore: two
        var maximize = titleBar.MaximizeButton.Bounds;
        PaintBackground(canvas, titleBar.MaximizeButton);
        var maxLeft = maximize.X + (maximize.Width - iconDimension) / 2f;
        var maxTop = maximize.Y + (maximize.Height - iconDimension) / 2f;
        var maxRect = new SKRect(maxLeft, maxTop, maxLeft + iconDimension, maxTop + iconDimension);
        if (isMaximized)
        {
            var offset = ToPixels(2, scaling);
            var backRect = new SKRect(maxRect.Left + offset, maxRect.Top - offset, maxRect.Right + offset, maxRect.Bottom - offset);
            canvas.DrawRect(backRect, iconStrokePaint);
        }
        canvas.DrawRect(maxRect, iconStrokePaint);

        // Close: an X
        var close = titleBar.CloseButton.Bounds;
        PaintBackground(canvas, titleBar.CloseButton);
        var closeLeft = close.X + (close.Width - iconDimension) / 2f;
        var closeTop = close.Y + (close.Height - iconDimension) / 2f;
        var closeRight = closeLeft + iconDimension;
        var closeBottom = closeTop + iconDimension;
        canvas.DrawLine(closeLeft, closeTop, closeRight, closeBottom, iconStrokePaint);
        canvas.DrawLine(closeLeft, closeBottom, closeRight, closeTop, iconStrokePaint);
    }

    static void PaintBackground(SKCanvas canvas, TitleBarButton button)
    {
        using var paint = new SKPaint { Color = button.IsHovered ? HoverBackground : IdleBackground, IsStroke = false, IsAntialias = false };
        canvas.DrawRect(button.Bounds.Left, button.Bounds.Top, button.Bounds.Width, button.Bounds.Height, paint);
    }

    // As Windows Forms rounds DIP to device units
    static int ToPixels(int dip, float scaling) => (int)Math.Round((double)scaling * dip);
}