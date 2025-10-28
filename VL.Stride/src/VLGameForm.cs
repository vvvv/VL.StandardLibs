using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Windows.Forms;
using VL.Core;
using VL.Core.Windows;
using VL.Stride.Games;

namespace VL.Stride;

internal class VLGameForm : GameForm, IHasCustomTitleBar
{
    private readonly Win32CustomTitleBar customTitleBar;
    private SpriteBatch spriteBatch;
    private Texture whiteTexture;
    private bool isFullscreen;

    public VLGameForm(NodeContext nodeContext, Win32CustomTitleBar.Options options)
    {
        customTitleBar = Win32CustomTitleBar.Install(this, nodeContext, options with { IsFullscreen = () => isFullscreen });
    }

    /// <summary>
    /// Gets or sets the title bar button icon color. Default is white.
    /// </summary>
    public Color TitleBarButtonColor { get; set; } = new Color(255, 255, 255, 255);

    /// <summary>
    /// Gets or sets the title bar button hover background color. Default is semi-transparent light gray.
    /// </summary>
    public Color TitleBarButtonHoverColor { get; set; } = new Color(170, 170, 170, 140);

    /// <summary>
    /// Gets or sets the title bar button idle background color. Default is semi-transparent dark gray.
    /// </summary>
    public Color TitleBarButtonIdleColor { get; set; } = new Color(60, 60, 60, 90);

    protected override void WndProc(ref Message m)
    {
        // Let the custom title bar handle Win32 messages
        if (customTitleBar.ProcessMessage(ref m) == true)
            return;

        base.WndProc(ref m);
    }

    /// <summary>
    /// Call this method from your rendering code to draw the title bar buttons.
    /// Pass in the GraphicsContext from your render loop.
    /// </summary>
    public void DrawTitleBarButtons(RenderDrawContext renderDrawContext)
    {
        if (!customTitleBar.ShouldDrawTitleBarButtons)
            return;

        var commandList = renderDrawContext.CommandList;

        // Initialize SpriteBatch and texture on first use
        if (spriteBatch == null)
            spriteBatch = new SpriteBatch(commandList.GraphicsDevice);

        if (whiteTexture == null)
            whiteTexture = renderDrawContext.GraphicsDevice.GetSharedWhiteTexture();

        var e = customTitleBar;
        var iconDimension = customTitleBar.LogicalToDeviceUnits(10);

        // Begin sprite batch in screen space
        spriteBatch.VirtualResolution = new Vector3(ClientSize.Width, ClientSize.Height, 1f);
        spriteBatch.Begin(renderDrawContext.GraphicsContext);

        try
        {
            // Helper to convert System.Drawing.Rectangle to RectangleF
            RectangleF ToRectF(System.Drawing.Rectangle rect) => 
                new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);

            // Draw minimize button
            if (e.MinimizeButtonHovered)
                spriteBatch.Draw(whiteTexture, ToRectF(e.MinimizeButtonRect), TitleBarButtonHoverColor);
            else
                spriteBatch.Draw(whiteTexture, ToRectF(e.MinimizeButtonRect), TitleBarButtonIdleColor);
            
            // Draw minimize icon (horizontal line)
            var minY = e.MinimizeButtonRect.Y + (e.MinimizeButtonRect.Height - 1) / 2f;
            var minX = e.MinimizeButtonRect.X + (e.MinimizeButtonRect.Width - iconDimension) / 2f;
            spriteBatch.Draw(whiteTexture, new RectangleF(minX, minY, iconDimension, 1), TitleBarButtonColor);

            // Draw maximize button
            if (e.MaximizeButtonHovered)
                spriteBatch.Draw(whiteTexture, ToRectF(e.MaximizeButtonRect), TitleBarButtonHoverColor);
            else
                spriteBatch.Draw(whiteTexture, ToRectF(e.MaximizeButtonRect), TitleBarButtonIdleColor);

            // Draw maximize icon (square or double square)
            var maxLeft = e.MaximizeButtonRect.X + (e.MaximizeButtonRect.Width - iconDimension) / 2f;
            var maxTop = e.MaximizeButtonRect.Y + (e.MaximizeButtonRect.Height - iconDimension) / 2f;
            
            if (e.IsMaximized)
            {
                // Draw two overlapping squares
                var offset = customTitleBar.LogicalToDeviceUnits(2);
                // Back square
                DrawRectOutline(spriteBatch, whiteTexture, maxLeft + offset, maxTop - offset, iconDimension, iconDimension, TitleBarButtonColor);
                // Front square
                DrawRectOutline(spriteBatch, whiteTexture, maxLeft, maxTop, iconDimension, iconDimension, TitleBarButtonColor);
            }
            else
            {
                // Single square
                DrawRectOutline(spriteBatch, whiteTexture, maxLeft, maxTop, iconDimension, iconDimension, TitleBarButtonColor);
            }

            // Draw close button
            if (e.CloseButtonHovered)
                spriteBatch.Draw(whiteTexture, ToRectF(e.CloseButtonRect), TitleBarButtonHoverColor);
            else
                spriteBatch.Draw(whiteTexture, ToRectF(e.CloseButtonRect), TitleBarButtonIdleColor);

            // Draw close icon (X)
            var closeLeft = e.CloseButtonRect.X + (e.CloseButtonRect.Width - iconDimension) / 2f;
            var closeTop = e.CloseButtonRect.Y + (e.CloseButtonRect.Height - iconDimension) / 2f;
            DrawLine(spriteBatch, whiteTexture, closeLeft, closeTop, closeLeft + iconDimension, closeTop + iconDimension, TitleBarButtonColor);
            DrawLine(spriteBatch, whiteTexture, closeLeft, closeTop + iconDimension, closeLeft + iconDimension, closeTop, TitleBarButtonColor);
        }
        finally
        {
            spriteBatch.End();
        }
    }

    // Helper method to draw a rectangle outline
    private void DrawRectOutline(SpriteBatch spriteBatch, Texture texture, float x, float y, float width, float height, Color color)
    {
        const float thickness = 1f;
        // Top
        spriteBatch.Draw(texture, new RectangleF(x, y, width, thickness), color);
        // Bottom
        spriteBatch.Draw(texture, new RectangleF(x, y + height - thickness, width, thickness), color);
        // Left
        spriteBatch.Draw(texture, new RectangleF(x, y, thickness, height), color);
        // Right
        spriteBatch.Draw(texture, new RectangleF(x + width - thickness, y, thickness, height), color);
    }

    // Helper method to draw a line
    private void DrawLine(SpriteBatch spriteBatch, Texture texture, float x1, float y1, float x2, float y2, Color color)
    {
        var distance = (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        var angle = (float)Math.Atan2(y2 - y1, x2 - x1);
        if (angle < 0)
        {
            x1 -= 0.5f;
            y1 -= 0.5f;
        }

        spriteBatch.Draw(texture,
            new RectangleF(x1, y1, distance, 1f),
            null,
            color,
            angle,
            new Vector2(0f, 0f));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            spriteBatch?.Dispose();
            spriteBatch = null;
        }
        base.Dispose(disposing);
    }
}
