#nullable enable
using Stride.Core.Mathematics;
using VL.Core;
using VL.Core.Import;
using VL.Skia;
using VL.UI.Core;

namespace Graphics.Skia;

[Smell(SymbolSmell.Experimental)]
public sealed class FormBoundsNotification
{
    private readonly RectangleF _controlBoundsDip;
    private readonly Vector2 _clientSizePx;
    private readonly System.Windows.Forms.Form _form;

    /// <summary>
    /// Create (stores initial control bounds in DIP and the Form reference).
    /// </summary>
    public FormBoundsNotification(RectangleF controlBounds, System.Windows.Forms.Form form)
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
        out System.Windows.Forms.Form? form)
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

    private static RectangleF ToPixels(RectangleF dip)
    {
        return DIPHelpers.DIPToPixel(dip);
    }

    private static Vector2 ToDIP(Vector2 pixels)
    {
        return new Vector2(pixels.X / CallerInfo.DIPFactor, pixels.Y / CallerInfo.DIPFactor);
    }
}