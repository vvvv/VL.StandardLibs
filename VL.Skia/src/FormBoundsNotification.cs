#nullable enable
using Stride.Core.Mathematics;
using VL.Core;
using VL.Core.Import;
using VL.Skia;

namespace Graphics.Skia;

/// <summary>Bounds of the renderer window.</summary>
[Smell(SymbolSmell.Experimental)]
public sealed class FormBoundsNotification
{
    private readonly RectangleF _controlBoundsDip;
    private readonly SkiaRenderer _renderer;
    private readonly Vector2 _clientSizePx;
    private readonly float _scale;

    /// <summary>
    /// Create (stores the outer bounds of the window in DIP and the renderer).
    /// </summary>
    public FormBoundsNotification(RectangleF controlBounds, SkiaRenderer renderer)
    {
        _controlBoundsDip = controlBounds;
        _renderer = renderer;
        var size = renderer.IsDisposed ? default : renderer.Window.ClientSize;
        _clientSizePx = new Vector2(size.X, size.Y);
        _scale = renderer.IsDisposed ? 1f : renderer.Window.ScaleFactor;
    }

    public void Split(
        out RectangleF controlBoundsPx,
        out RectangleF controlBoundsDIP,
        out Vector2 clientSizePx,
        out Vector2 clientSizeDIP,
        out SkiaRenderer? form)
    {
        form = _renderer;
        controlBoundsDIP = _controlBoundsDip;
        controlBoundsPx = new RectangleF(_controlBoundsDip.X * _scale, _controlBoundsDip.Y * _scale, _controlBoundsDip.Width * _scale, _controlBoundsDip.Height * _scale);
        clientSizePx = _clientSizePx;
        clientSizeDIP = _clientSizePx / _scale;
    }
}
