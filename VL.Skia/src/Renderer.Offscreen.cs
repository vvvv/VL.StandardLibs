using SkiaSharp;
using System;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;
using Stride.Core.Mathematics;
using VL.Lib.Basics.Resources;
using System.Threading;
using VL.Skia.Egl;

namespace VL.Skia
{
    public sealed class OffScreenRenderer : IDisposable
    {
        private readonly RenderContext renderContext;
        private readonly Thread renderThread;

        private readonly Producing<SKImage> output = new Producing<SKImage>();
        private Int2 size;
        private SKSurface surface;
        private Int2 surfaceSize;
        private SKCanvas canvas;
        private EglSurface eglSurface;

        public OffScreenRenderer()
        {
            renderContext = RenderContext.ForCurrentThread();
            renderThread = Thread.CurrentThread;
        }

        public ILayer Layer { get; set; }

        public Int2 Size
        {
            get => size;
            set => size = new Int2(Math.Max(value.X, 1), Math.Max(value.Y, 1));
        }

        IDisposable FMouseSubscription;
        IMouse FMouse;
        public IMouse Mouse
        {
            get { return FMouse; }
            set
            {
                if (value != FMouse)
                {
                    FMouseSubscription?.Dispose();
                    FMouse = value;
                    if (value != null)
                        FMouseSubscription = FMouse.Notifications.Subscribe(Notify);
                }
            }
        }

        IDisposable FKeyboardSubscription;
        IKeyboard FKeyboard;
        public IKeyboard Keyboard
        {
            get { return FKeyboard; }
            set
            {
                if (value != FKeyboard)
                {
                    FKeyboardSubscription?.Dispose();
                    FKeyboard = value;
                    if (value != null)
                        FKeyboardSubscription = FKeyboard.Notifications.Subscribe(Notify);
                }
            }
        }

        void Notify(INotification n)
        {
            var canvas = surface?.Canvas;
            if (canvas != null)
                Layer?.Notify(n, CallerInfo.InRenderer(Size.X, Size.Y, canvas, renderContext.SkiaContext));
        }

        public SKImage Update(ILayer layer, IMouse mouse, IKeyboard keyboard, int width = 400, int height = 300)
        {
            Mouse = mouse;
            Keyboard = keyboard;
            Size = new Int2(width, height);
            Layer = layer;

            return Render();
        }

        SKImage Render()
        {
            if (Thread.CurrentThread != renderThread)
                throw new InvalidOperationException("Render must be called on the same thread as where the renderer has been created in.");

            // Make our render context the current one
            using var _ = renderContext.MakeCurrent();

            // Release the previously rendered image. If no other consumers are present this will allow us to re-use the backing texture for this render pass.
            output.Resource = null;

            // Create offscreen surface to render into
            var size = Size;
            if (surface is null || size != surfaceSize)
            {
                surfaceSize = size;
                surface?.Dispose();
                //eglSurface?.Dispose();
                //eglSurface = renderContext.EglContext.CreatePbufferSurface(size.X, size.Y);
                var info = new SKImageInfo(size.X, size.Y, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
                surface = SKSurface.Create(renderContext.SkiaContext, budgeted: false, info, sampleCount: 0, origin: GRSurfaceOrigin.TopLeft, null, shouldCreateWithMips: true);
                //using (renderContext.MakeCurrent(eglSurface))
                //{
                //    surface = CreateSkSurface(renderContext, eglSurface);
                //}
                canvas = surface.Canvas;
            }


            // Render
            using (new SKAutoCanvasRestore(canvas, true))
            {
                Layer?.Render(CallerInfo.InRenderer(size.X, size.Y, canvas, renderContext.SkiaContext));
            }
            surface.Flush();

            // Ref count the rendered image. As far as we're concerned it shall be valid until the next frame.
            // So if no one was interested in it, it will be disposed and the backing texture will go back into the texture pool.
            var image = output.Resource = surface.Snapshot();

            //renderContext.EglContext.ReleaseCurrent();

            return image;
        }

        public void Dispose()
        {
            //renderContext.MakeCurrent();

            FMouseSubscription?.Dispose();
            FKeyboardSubscription?.Dispose();
            output.Dispose();
            surface?.Dispose();
        }

        SKSurface CreateSkSurface(RenderContext context, EglSurface eglSurface)
        {
            var colorType = SKColorType.Rgba8888;
            NativeGles.glGetIntegerv(NativeGles.GL_FRAMEBUFFER_BINDING, out var framebuffer);
            NativeGles.glGetIntegerv(NativeGles.GL_STENCIL_BITS, out var stencil);
            NativeGles.glGetIntegerv(NativeGles.GL_SAMPLES, out var samples);
            var maxSamples = context.SkiaContext.GetMaxSurfaceSampleCount(colorType);
            if (samples > maxSamples)
                samples = maxSamples;

            var glInfo = new GRGlFramebufferInfo(
                fboId: (uint)framebuffer,
                format: colorType.ToGlSizedFormat());

            using var renderTarget = new GRBackendRenderTarget(
                width: eglSurface.Size.X,
                height: eglSurface.Size.Y,
                sampleCount: samples,
                stencilBits: stencil,
                glInfo: glInfo);

            var useLinearColorspace = false;
            if (context.UseLinearColorspace)
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
                context.SkiaContext,
                renderTarget,
                GRSurfaceOrigin.TopLeft,
                colorType,
                colorspace: useLinearColorspace ? SKColorSpace.CreateSrgbLinear() : SKColorSpace.CreateSrgb());
        }
    }
}
