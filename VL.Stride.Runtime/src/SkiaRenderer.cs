using SharpDX.Direct3D11;
using SkiaSharp;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using System.Reactive.Disposables;
using VL.Core;
using VL.Skia;
using VL.Skia.Egl;
using VL.Stride.Input;
using VL.UI.Core;
using PixelFormat = Stride.Graphics.PixelFormat;
using SkiaRenderContext = VL.Skia.RenderContext;

namespace VL.Stride
{
    /// <summary>
    /// Renders the Skia layer into the Stride provided surface.
    /// </summary>
    [ProcessNode(Category = "Stride.Rendering", FragmentSelection = FragmentSelection.Explicit)]
    public partial class SkiaRenderer : RendererBase
    {
        private static readonly SKColorSpace srgbLinearColorspace = SKColorSpace.CreateSrgbLinear();
        private static readonly SKColorSpace srgbColorspace = SKColorSpace.CreateSrgb();

        private readonly SerialDisposable inputSubscription = new SerialDisposable();
        private readonly ILogger logger;
        private IInputSource lastInputSource;
        private Int2 lastRenderTargetSize;
        private readonly InViewportUpstream viewportLayer = new InViewportUpstream();
        private readonly SetSpaceUpstream2 withinCommonSpaceLayer = new SetSpaceUpstream2();

        // To access render targets with multi sampling enabled we need a custom EGL context
        private EglContext msaaAwareEglContext;

        public ILayer Layer { get; set; }

        public CommonSpace Space { get; set; }

        [Fragment]
        public SkiaRenderer(NodeContext nodeContext)
        {
            logger = nodeContext.GetLogger();
        }

        [Fragment]
        [return: Pin(Name = "Output")]
        public IGraphicsRendererBase Update(ILayer input, CommonSpace space)
        {
            Layer = input;
            Space = space;
            return this;
        }

        protected override void Destroy()
        {
            inputSubscription.Dispose();
            msaaAwareEglContext?.Dispose();
            base.Destroy();
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            if (Layer is null)
                return;

            var commandList = context.CommandList;
            var renderTarget = commandList.RenderTarget;
            var sampleCount = (int)renderTarget.MultisampleCount;

            // Fetch the skia render context (uses ANGLE -> DirectX11)
            var skiaRenderContext = SkiaRenderContext.ForCurrentApp();
            if (sampleCount > 1)
            {
                var shareContext = skiaRenderContext.EglContext;
                if (msaaAwareEglContext is null || msaaAwareEglContext.SampleCount != sampleCount || msaaAwareEglContext.ShareContext != shareContext)
                {
                    logger.LogWarning("Rendering to a multi-sampled target (Samples={Samples}) may be unstable on some machines. Use the AfterScene stage for rendering when used with RenderEntity node.", sampleCount);

                    msaaAwareEglContext?.Dispose();
                    msaaAwareEglContext = EglContext.New(shareContext.Display, sampleCount, shareContext);
                }
            }
            else
            {
                msaaAwareEglContext?.Dispose();
                msaaAwareEglContext = null;
            }

            // Subscribe to input events - in case we have many sinks we assume that there's only one input source active
            var renderTargetSize = new Int2(renderTarget.Width, renderTarget.Height);
            var inputSource = context.RenderContext.GetWindowInputSource();
            if (inputSource != lastInputSource || renderTargetSize != lastRenderTargetSize)
            {
                lastInputSource = inputSource;
                lastRenderTargetSize = renderTargetSize;
                inputSubscription.Disposable = SubscribeToInputSource(inputSource, context, canvas: null, skiaRenderContext.SkiaContext, DIPHelpers.DIPFactor() /* FIXME */);
            }

            // Make current on current thread for resource creation
            EglSurface eglSurface;
            var eglContext = msaaAwareEglContext ?? skiaRenderContext.EglContext;
            using (eglContext.MakeCurrent(forRendering: false))
            {
                var nativeTempRenderTarget = SharpDXInterop.GetNativeResource(renderTarget) as Texture2D;
                eglSurface = eglContext.CreateSurfaceFromClientBuffer(nativeTempRenderTarget.NativePointer);
            }
            // Make the surface current (becomes default FBO)
            using (eglSurface)
            using (skiaRenderContext.MakeCurrent(forRendering: true, eglSurface))
            {
                // Setup a skia surface around the currently set render target
                using var surface = CreateSkSurface(skiaRenderContext.SkiaContext, renderTarget);

                // Render
                var canvas = surface.Canvas;
                var viewport = context.RenderContext.ViewportState.Viewport0;
                canvas.ClipRect(SKRect.Create(viewport.X, viewport.Y, viewport.Width, viewport.Height));
                withinCommonSpaceLayer.Update(Layer, out var spaceLayer, Space);
                viewportLayer.Update(spaceLayer, SKRect.Create(viewport.X, viewport.Y, viewport.Width, viewport.Height), CommonSpace.PixelTopLeft, out var layer);

                layer.Render(CallerInfo.InRenderer(renderTarget.Width, renderTarget.Height, canvas, skiaRenderContext.SkiaContext, DIPHelpers.DIPFactor() /* FIXME */));

                // Flush
                surface.Flush();
            }
        }

        SKSurface CreateSkSurface(GRContext context, Texture texture)
        {
            var colorType = PixelFormatHelper.ToSKColorType(texture.ViewFormat);
            NativeGles.glGetIntegerv(NativeGles.GL_STENCIL_BITS, out var stencil);
            NativeGles.glGetIntegerv(NativeGles.GL_SAMPLES, out var samples);
            var maxSamples = context.GetMaxSurfaceSampleCount(colorType);
            if (samples > maxSamples)
                samples = maxSamples;

            var glInfo = new GRGlFramebufferInfo(
                fboId: (uint)0, // This is intentional - FBO 0 is used because we bind the EGL surface as the default framebuffer
                format: colorType.ToGlSizedFormat());

            using var renderTarget = new GRBackendRenderTarget(
                width: texture.Width,
                height: texture.Height,
                sampleCount: samples,
                stencilBits: stencil,
                glInfo: glInfo);

            var useLinearColorspace = false;
            if (GraphicsDevice.ColorSpace == ColorSpace.Linear && IsLinear(texture.ViewFormat) && GetResourceFormat(texture) == texture.ViewFormat)
            {
                // Output looks correct in the following cases:
                // - Rendering to swap chain, the render target is non-srgb while the view is srgb
                // - Rendering to a typeless texture with a srgb view
                // In those cases we can assume the hardware is taking care of interpreting the bits correctly.

                // In all other cases we can somewhat "fix" the colors by telling Skia to use a linear colorspace,
                // but alpha blending is still somewhat broken leading to wrong output. For example use the "Randomwalk" Skia help patch.
                
                // Blending results are even worse when using a floating point texture. This also seems independent of the used color space.

                // TODO: Should we change the default of the RenderTexture node to use a typeless format? Or at least adjust the SkiaTexture node?
                // TODO: Re-evaluate this part once Skia has the srgb flags (see comment in https://discourse.vvvv.org/t/combining-stride-and-skia-render-engine/19798/8)
                useLinearColorspace = true;
            }

            return SKSurface.Create(
                context, 
                renderTarget, 
                GRSurfaceOrigin.TopLeft, 
                colorType, 
                colorspace: useLinearColorspace ? srgbLinearColorspace : srgbColorspace);
        }

        static bool IsLinear(PixelFormat pixelFormat) => pixelFormat.IsSRgb() || pixelFormat.IsHDR();

        static PixelFormat GetResourceFormat(Texture texture)
        {
            // Since Stride switched to the new flip model, the backbuffer has a non-srgb format and only the view has a srgb format.
            // See for example https://walbourn.github.io/care-and-feeding-of-modern-swapchains/ where it explains that this is the recommended
            // setup in DirectX 11 and even required in DirectX 12.
            // Stride hides that fact from us, we therefor need to query the underlying API.
            var nativeTexture = SharpDXInterop.GetNativeResource(texture) as Texture2D;
            return nativeTexture != null ? (PixelFormat)nativeTexture.Description.Format : texture.Format;
        }
    }
}
