#nullable enable
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Import;
using VL.Skia.Egl;

namespace VL.Skia;

[ProcessNode(Category = "Graphics.Skia.Imaging.Advanced")]
public sealed class FromSharedHandle : IDisposable
{
    private readonly RenderContextProvider renderContextProvider;
    private nint textureHandle;
    private RenderContext? renderContext;
    private SKImage? skImage;

    public FromSharedHandle()
    {
        renderContextProvider = AppHost.Current.GetRenderContextProvider();
    }

    public SKImage? Update(nint textureHandle)
    {
        var renderContext = renderContextProvider.GetRenderContext();
        if (textureHandle != this.textureHandle || renderContext != this.renderContext)
        {
            this.textureHandle = textureHandle;
            this.renderContext = renderContext;

            Interlocked.Exchange(ref skImage, null)?.Dispose();

            if (textureHandle != 0)
            {
                using var _ = renderContext.MakeCurrent(forRendering: false);
                skImage = D3D11Utils.SharedHandleToSKImage(renderContext, textureHandle);
            }

        }
        return skImage;
    }

    public void Dispose()
    {
        skImage?.Dispose();
    }
}
