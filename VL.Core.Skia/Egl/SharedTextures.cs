#nullable enable
using SkiaSharp;
using System;
using System.Runtime.Versioning;
using System.Threading;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.System.Com;
using Windows.Win32.Graphics.Dxgi.Common;
using static Windows.Win32.Graphics.Dxgi.Common.DXGI_FORMAT;

namespace VL.Skia.Egl;

/// <summary>
/// Writer side of a texture which another process can open: a Direct3D 11 texture (BGRA8, render target and shader resource, legacy shared handle) created on the ANGLE device of the
/// <see cref="RenderContext"/>, wrapped as the default framebuffer of an EGL pbuffer surface (<c>EGL_D3D_TEXTURE_ANGLE</c>) so that Skia renders straight into it. After a render the commands
/// are flushed and an event query tells when the GPU is done, so that a frame announced to the reader is complete.
/// </summary>
[SupportedOSPlatform("windows8.0")]
public sealed unsafe class SharedTextureTarget : IDisposable
{
    private readonly RenderContext renderContext;
    private ID3D11Texture2D* texture;
    private ID3D11Query* query;
    private ID3D11DeviceContext* immediateContext;
    private EglSurface? eglSurface;
    private SKSurface? skSurface;

    public int Width { get; }
    public int Height { get; }

    /// <summary>Legacy shared handle (<c>IDXGIResource::GetSharedHandle</c>): a system wide value, any process of the session can open it with <c>OpenSharedResource</c>, no duplication needed.</summary>
    public nint SharedHandle { get; }

    public SharedTextureTarget(RenderContext renderContext, int width, int height)
    {
        this.renderContext = renderContext;
        Width = width;
        Height = height;

        if (!renderContext.EglContext.Display.TryGetD3D11DeviceInterface(out var device))
            throw new InvalidOperationException("The render context has no Direct3D 11 device.");

        var desc = new D3D11_TEXTURE2D_DESC
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = DXGI_FORMAT_B8G8R8A8_UNORM,
            SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
            Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
            BindFlags = D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET | D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE,
            MiscFlags = D3D11_RESOURCE_MISC_FLAG.D3D11_RESOURCE_MISC_SHARED,
        };
        ID3D11Texture2D* tex;
        device->CreateTexture2D(&desc, null, &tex);
        texture = tex;

        IDXGIResource* dxgiResource;
        tex->QueryInterface(in IDXGIResource.IID_Guid, out var r).ThrowOnFailure();
        dxgiResource = (IDXGIResource*)r;
        try
        {
            Windows.Win32.Foundation.HANDLE handle;
            dxgiResource->GetSharedHandle(&handle);
            SharedHandle = (nint)handle.Value;
        }
        finally
        {
            dxgiResource->Release();
        }

        var qd = new D3D11_QUERY_DESC { Query = D3D11_QUERY.D3D11_QUERY_EVENT };
        ID3D11Query* q;
        device->CreateQuery(&qd, &q);
        query = q;

        ID3D11DeviceContext* ctx;
        device->GetImmediateContext(&ctx);
        immediateContext = ctx;

        using var _ = renderContext.MakeCurrent(forRendering: false);
        eglSurface = renderContext.EglContext.CreateSurfaceFromClientBuffer((nint)texture);
    }

    /// <summary>Renders with <paramref name="draw"/> into the texture, flushes, and tells the GPU to signal completion (see <see cref="IsGpuDone"/>).</summary>
    public void Render(Action<CallerInfo> draw, float scaling)
    {
        ObjectDisposedException.ThrowIf(eglSurface is null, this);
        using var _ = renderContext.MakeCurrent(forRendering: true, eglSurface);

        if (skSurface is null)
        {
            var colorType = SKColorType.Rgba8888;
            using var renderTarget = new GRBackendRenderTarget(Width, Height, sampleCount: 0, stencilBits: 0,
                glInfo: new GRGlFramebufferInfo(fboId: 0, format: colorType.ToGlSizedFormat()));
            skSurface = SKSurface.Create(renderContext.SkiaContext, renderTarget, GRSurfaceOrigin.TopLeft, colorType, colorspace: SKColorSpace.CreateSrgb())
                ?? throw new InvalidOperationException("Could not create the Skia surface on the shared texture.");
        }

        var callerInfo = CallerInfo.InRenderer(Width, Height, skSurface, renderContext.SkiaContext, scaling);
        using (new SKAutoCanvasRestore(skSurface.Canvas, true))
            draw(callerInfo);
        skSurface.Flush();
        NativeGles.glFlush();
        immediateContext->End((ID3D11Asynchronous*)query);
        immediateContext->Flush();
    }

    /// <summary>Copies the pixels of the last <see cref="Render"/> to memory (BGRA, premultiplied): the transport alternative to sharing the texture. Measured by the spike.</summary>
    public bool ReadPixels(nint destination, int rowBytes)
    {
        ObjectDisposedException.ThrowIf(eglSurface is null, this);
        using var _ = renderContext.MakeCurrent(forRendering: true, eglSurface);
        return skSurface!.ReadPixels(new SKImageInfo(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul), destination, rowBytes, 0, 0);
    }

    /// <summary>True when the GPU has executed everything of the last <see cref="Render"/>.</summary>
    public bool IsGpuDone()
    {
        int done = 0;
        var hr = immediateContext->GetData((ID3D11Asynchronous*)query, &done, 4, 0);
        return hr.Value == 0 && done != 0;
    }

    /// <summary>Waits until <see cref="IsGpuDone"/>, returns false after the timeout.</summary>
    public bool WaitForGpu(int timeoutMs = 100)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var spin = new SpinWait();
        while (!IsGpuDone())
        {
            if (sw.ElapsedMilliseconds > timeoutMs)
                return false;
            spin.SpinOnce(sleep1Threshold: -1);
        }
        return true;
    }

    public void Dispose()
    {
        try
        {
            if (eglSurface is not null)
            {
                using var _ = renderContext.MakeCurrent(forRendering: false);
                skSurface?.Dispose();
                skSurface = null;
                eglSurface.Dispose();
                eglSurface = null;
            }
        }
        catch (Exception) { }
        if (query != null) { ((IUnknown*)query)->Release(); query = null; }
        if (immediateContext != null) { ((IUnknown*)immediateContext)->Release(); immediateContext = null; }
        if (texture != null) { ((IUnknown*)texture)->Release(); texture = null; }
    }
}

/// <summary>Reader side: opens the shared handle of a <see cref="SharedTextureTarget"/> of another process on the ANGLE device of a render context.</summary>
public static class SharedTexture
{
    /// <summary>
    /// Opens the legacy shared handle on the Direct3D 11 device of the render context and wraps it as an image. The image stays valid, and keeps showing the last content written,
    /// when the process which created the texture dies (the texture lives as long as one device references it). Call with the render context current.
    /// </summary>
    [SupportedOSPlatform("windows6.1")]
    public static SKImage? Open(RenderContext renderContext, nint sharedHandle)
    {
        using var _ = renderContext.MakeCurrent(forRendering: false);
        return D3D11Utils.SharedHandleToSKImage(renderContext, sharedHandle);
    }
}
