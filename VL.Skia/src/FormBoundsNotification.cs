#nullable enable
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