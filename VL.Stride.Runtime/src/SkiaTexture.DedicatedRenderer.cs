#nullable enable

using SkiaSharp;
using Stride.Graphics;
using System.Runtime.Versioning;
using VL.Core;
using VL.Skia;
using VL.Skia.Egl;
using VL.Stride.Games;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D11;

namespace VL.Stride;

partial class SkiaTexture
{
    /// <summary>
    /// Per-app-host service that renders Skia textures in batch during Stride's draw call.
    /// </summary>
    [SupportedOSPlatform("Windows6.1")]
    sealed unsafe class DedicatedRenderer : Renderer
    {
        private readonly AppHost appHost;
        private readonly VLGame game;

        public ID3D11Device* skiaDevice;
        private ID3D11DeviceContext4* skiaDeviceContext;
        private ID3D11DeviceContext4* strideDeviceContext;

        // Global fences for pipeline synchronization
        // Skia signals this when done writing (rendering/copying) - Stride waits on it before reading
        private ID3D11Fence* skiaWriteCompleteFence;
        private ID3D11Fence* skiaWriteCompleteFenceOnStride;
        private ulong skiaWriteCompleteFenceValue;

        // Stride signals this when done reading - Skia waits on it before writing
        private ID3D11Fence* strideReadCompleteFence;
        private ID3D11Fence* strideReadCompleteFenceOnSkia;
        private ulong strideReadCompleteFenceValue;

        private DedicatedRenderer(AppHost appHost, VLGame game, RenderContextProvider renderContextProvider)
            : base(renderContextProvider)
        {
            this.appHost = appHost;
            this.game = game;

            // Initialize global fences
            var renderContext = renderContextProvider.GetRenderContext();
            if (renderContext.EglContext.Display.TryGetD3D11DeviceInterface(out skiaDevice) &&
                SharpDXInterop.GetNativeDevice(game.GraphicsDevice) is SharpDX.Direct3D11.Device strideNativeDevice)
            {
                var strideDevice = (ID3D11Device*)strideNativeDevice.NativePointer;

                skiaDeviceContext = GetDeviceContext(skiaDevice);
                strideDeviceContext = GetDeviceContext(strideDevice);

                // Skia write complete fence (Skia signals, Stride waits)
                skiaWriteCompleteFence = CreateFence(skiaDevice);
                HANDLE handle1 = default;
                skiaWriteCompleteFence->CreateSharedHandle(null, 0x80000000, null, &handle1);
                skiaWriteCompleteFenceOnStride = OpenFence(strideDevice, handle1);

                // Stride read complete fence (Stride signals, Skia waits)
                strideReadCompleteFence = CreateFence(strideDevice);
                HANDLE handle2 = default;
                strideReadCompleteFence->CreateSharedHandle(null, 0x80000000, null, &handle2);
                strideReadCompleteFenceOnSkia = OpenFence(skiaDevice, handle2);

                skiaWriteCompleteFenceValue = 0;
                strideReadCompleteFenceValue = 0;

                game.DrawStarted += OnDrawStarted;
                game.DrawEnded += OnDrawEnded;
            }
        }

        public static DedicatedRenderer GetOrCreate(AppHost appHost, VLGame game)
        {
            return appHost.Services.GetOrAddService(_ =>
            {
                var renderContextProvider = appHost.GetRenderContextProvider(dedicated: true);
                return new DedicatedRenderer(appHost, game, renderContextProvider);
            }, allowToAskParent: false);
        }

        private void OnDrawStarted(object? sender, EventArgs e)
        {
            try
            {
                var renderContext = RenderContextProvider.GetRenderContext();

                // Wait for Stride to finish reading from previous frame
                skiaDeviceContext->Wait(strideReadCompleteFenceOnSkia, strideReadCompleteFenceValue);

                foreach (var texture in renderList)
                {
                    using (renderContext.MakeCurrent(forRendering: false, texture.eglSurface))
                        texture.Render(renderContext.SkiaContext, texture.skSurface!);
                }

                // Signal once that Skia is done writing
                skiaWriteCompleteFenceValue++;
                skiaDeviceContext->Signal(skiaWriteCompleteFence, skiaWriteCompleteFenceValue);

                // Flush Skia context to ensure all commands are sent
                skiaDeviceContext->Flush();

                // Wait for Skia writes to complete before we enter Stride rendering
                strideDeviceContext->Wait(skiaWriteCompleteFenceOnStride, skiaWriteCompleteFenceValue);

            }
            catch (Exception ex)
            {
                appHost.DefaultLogger.LogError(ex, "Error during SkiaTexture rendering");
            }
            finally
            {
                renderList.Clear();
            }
        }

        private void OnDrawEnded(object? sender, EventArgs e)
        {
            try
            {
                // Signal that Stride is done reading
                strideReadCompleteFenceValue++;
                strideDeviceContext->Signal(strideReadCompleteFence, strideReadCompleteFenceValue);
            }
            catch (Exception ex)
            {
                appHost.DefaultLogger.LogError(ex, "Error signaling Stride read complete");
            }
        }

        public override void Dispose()
        {
            game.DrawStarted -= OnDrawStarted;
            game.DrawEnded -= OnDrawEnded;

            // Clean up fences
            ReleaseFence(ref skiaWriteCompleteFence);
            ReleaseFence(ref skiaWriteCompleteFenceOnStride);
            ReleaseFence(ref strideReadCompleteFence);
            ReleaseFence(ref strideReadCompleteFenceOnSkia);

            ReleaseDeviceContext(ref skiaDeviceContext);
            ReleaseDeviceContext(ref strideDeviceContext);
        }

        public override void CreateSkiaSurfaces(Texture texture, out EglSurface eglSurface, out SKSurface skSurface)
        {
            var renderContext = RenderContextProvider.GetRenderContext();

            var id = ID3D11Texture2D.IID_Guid;
            ID3D11Texture2D* textureOnSkia;
            skiaDevice->OpenSharedResource((HANDLE)texture.SharedHandle, &id, (void**)&textureOnSkia);
            if (textureOnSkia is null)
                throw new Exception("Failed to open shared texture on Skia device");

            try
            {
                eglSurface = renderContext.EglContext.CreateSurfaceFromClientBuffer((nint)textureOnSkia);
                var skColorType = SKColorType.Bgra8888;
                var glInfo = new GRGlFramebufferInfo(0, skColorType.ToGlSizedFormat());
                using var gRBackendRenderTarget = new GRBackendRenderTarget(texture.Width, texture.Height, 0, 0, glInfo);
                skSurface = SKSurface.Create(renderContext.SkiaContext, gRBackendRenderTarget, GRSurfaceOrigin.TopLeft, skColorType);
            }
            finally
            {
                textureOnSkia->Release();
            }
        }

        private static ID3D11DeviceContext4* GetDeviceContext(ID3D11Device* device)
        {
            ID3D11DeviceContext* deviceContext;
            device->GetImmediateContext(&deviceContext);
            try
            {
                deviceContext->QueryInterface<ID3D11DeviceContext4>(out var result);
                return result;
            }
            finally
            {
                deviceContext->Release();
            }
        }

        private static ID3D11Fence* CreateFence(ID3D11Device* device)
        {
            ID3D11Device5* device5;
            device->QueryInterface(out device5);
            if (device5 != null)
            {
                ID3D11Fence* fence;
                device5->CreateFence(0, D3D11_FENCE_FLAG.D3D11_FENCE_FLAG_SHARED, typeof(ID3D11Fence).GUID, (void**)&fence);
                device5->Release();
                return fence;
            }
            return null;
        }

        private static ID3D11Fence* OpenFence(ID3D11Device* device, HANDLE handle)
        {
            ID3D11Device5* device5;
            device->QueryInterface(out device5);
            if (device5 != null)
            {
                var id = ID3D11Fence.IID_Guid;
                ID3D11Fence* fence;
                device5->OpenSharedFence(handle, &id, (void**)&fence);
                device5->Release();
                return fence;
            }
            return null;
        }

        private static void ReleaseFence(ref ID3D11Fence* fence)
        {
            if (fence != null)
            {
                fence->Release();
                fence = null;
            }
        }

        private static void ReleaseDeviceContext(ref ID3D11DeviceContext4* deviceContext)
        {
            if (deviceContext != null)
            {
                deviceContext->Release();
                deviceContext = null;
            }
        }
    }
}