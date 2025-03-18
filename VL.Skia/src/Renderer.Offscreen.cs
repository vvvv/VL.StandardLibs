using SkiaSharp;
using System;
using VL.Lib.IO;
using VL.Lib.IO.Notifications;
using Stride.Core.Mathematics;
using VL.Lib.Basics.Resources;

namespace VL.Skia
{
    public sealed class OffScreenRenderer : IDisposable
    {
        private readonly RenderContext renderContext;

        private readonly Producing<SKImage> output = new Producing<SKImage>();
        private Int2 size;
        private SKSurface surface;
        private Int2 surfaceSize;

        public OffScreenRenderer()
        {
            renderContext = RenderContext.ForCurrentApp();
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
            // Make our render context the current one
            using var _ = renderContext.MakeCurrent(forRendering: true);

            // Release the previously rendered image. If no other consumers are present this will allow us to re-use the backing texture for this render pass.
            output.Resource = null;

            // Create offscreen surface to render into
            var size = Size;
            if (surface is null || size != surfaceSize)
            {
                surfaceSize = size;
                surface?.Dispose();
                var info = new SKImageInfo(size.X, size.Y, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
                surface = SKSurface.Create(renderContext.SkiaContext, budgeted: false, info, sampleCount: 0, origin: GRSurfaceOrigin.TopLeft, null, shouldCreateWithMips: true);
            }


            // Render
            var canvas = surface.Canvas;
            using (new SKAutoCanvasRestore(canvas, true))
            {
                Layer?.Render(CallerInfo.InRenderer(size.X, size.Y, canvas, renderContext.SkiaContext));
            }
            surface.Flush();

            // Ref count the rendered image. As far as we're concerned it shall be valid until the next frame.
            // So if no one was interested in it, it will be disposed and the backing texture will go back into the texture pool.
            return output.Resource = surface.Snapshot();
        }

        public void Dispose()
        {
            using var _ = renderContext.MakeCurrent(forRendering: false);

            FMouseSubscription?.Dispose();
            FKeyboardSubscription?.Dispose();

            output.Dispose();
            surface?.Dispose();
        }
    }
}
