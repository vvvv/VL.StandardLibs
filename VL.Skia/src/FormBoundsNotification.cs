#nullable enable
#if WINDOWS
extern alias sw;

using Stride.Core.Mathematics;
using System.Windows.Forms;
using sw::System.Windows.Forms;
using VL.Core;
using VL.Core.Import;

namespace Graphics.Skia;

[Smell(SymbolSmell.Experimental)]
public sealed class FormBoundsNotification
{
    private readonly RectangleF _controlBoundsDip;
    private readonly Vector2 _clientSizePx;
    private readonly Form _form;

    /// <summary>
    /// Create (stores initial control bounds in DIP and the Form reference).
    /// </summary>
    public FormBoundsNotification(RectangleF controlBounds, Form form)
    {
        _controlBoundsDip = controlBounds;
        _form = form;
        var sizePx = form.ClientSize;
        _clientSizePx = new Vector2(sizePx.Width, sizePx.Height);
    }

    public void Split(
        out RectangleF controlBoundsPx,
        out RectangleF controlBoundsDIP,
        out Vector2 clientSizePx,
        out Vector2 clientSizeDIP,
        out Form? form)
    {
        form = _form;

        // Control Bounds DIP (unchanged)
        controlBoundsDIP = _controlBoundsDip;

        // Control Bounds px (DIP -> Pixel)
        controlBoundsPx = ToPixels(_controlBoundsDip);

        // Client size Pixel
        clientSizePx = _clientSizePx;

        // Client size DIP
        clientSizeDIP = ToDIP(clientSizePx);
    }

    // Helpers mirroring the patch converters (DIPToPixel / PixelToDIP)

    private RectangleF ToPixels(RectangleF dip)
    {
        return _form.LogicalToDeviceUnits(dip);
    }

    private Vector2 ToDIP(Vector2 pixels)
    {
        return _form.DeviceToLogicalUnits(pixels);
    }
}
#else
using Stride.Core.Mathematics;
using VL.Core;
using VL.Core.Import;
using VL.Skia;

namespace Graphics.Skia;

/// <summary>Bounds of the renderer window: the flavor for systems without Windows Forms (the "form" is the <see cref="SkiaRenderer"/> of the desktop host).</summary>
[Smell(SymbolSmell.Experimental)]
public sealed class FormBoundsNotification
{
    private readonly RectangleF _controlBoundsDip;
    private readonly SkiaRenderer _renderer;
    private readonly Vector2 _clientSizePx;
    private readonly float _scale;

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
#endif
