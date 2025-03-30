using SkiaSharp;
using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Threading;
using VL.Core;
using VL.Skia.Egl;

namespace VL.Skia
{
    public unsafe sealed class RenderContext : IDisposable
    {
        public const int ResourceCacheLimit = 512 * 1024 * 1024;

        [Obsolete("Use ForCurrentApp() instead")]
        public static RenderContext ForCurrentThread() => ForCurrentApp();

        /// <summary>
        /// Returns the render context for the current app.
        /// </summary>
        public static unsafe RenderContext ForCurrentApp()
        {
            var appHost = AppHost.CurrentOrGlobal;
            return appHost.Services.GetOrAddService(s =>
            {
                var display = EglDisplay.ForCurrentApp();
                var renderContext = New(display);

                // For compatibility with existing code
                var lifetime = Disposable.Create(renderContext, ctx => ctx.DoDispose());
                lifetime.DisposeBy(appHost);

                return renderContext;
            }, allowToAskParent: false /* Please don't */);
        }

        public static RenderContext New(EglDisplay display, int sampleCount = 1, EglContext shareContext = null)
        {
            var context = EglContext.New(display, sampleCount, shareContext);
            using var _ = context.MakeCurrent(forRendering: false);
            var backendContext = GRGlInterface.CreateAngle();
            if (backendContext is null)
                throw new Exception("Failed to create ANGLE backend context");
            var skiaContext = GRContext.CreateGl(backendContext);
            if (skiaContext is null)
                throw new Exception("Failed to create Skia backend graphics context");

            // 512MB instead of the default 96MB
            skiaContext.SetResourceCacheLimit(ResourceCacheLimit);
            return new RenderContext(context, backendContext, skiaContext, sampleCount);
        }

        public readonly EglContext EglContext;
        public readonly GRContext SkiaContext;

        private readonly GRGlInterface BackendContext;
        private readonly Thread thread;

        RenderContext(EglContext eglContext, GRGlInterface backendContext, GRContext skiaContext, int sampleCount)
        {
            EglContext = eglContext ?? throw new ArgumentNullException(nameof(eglContext));
            BackendContext = backendContext ?? throw new ArgumentNullException(nameof(backendContext));
            SkiaContext = skiaContext ?? throw new ArgumentNullException(nameof(skiaContext));
            SampleCount = sampleCount;
            thread = Thread.CurrentThread;
        }

        public bool UseLinearColorspace => EglContext.Display.UseLinearColorspace;

        public bool IsOnCorrectThread => Thread.CurrentThread == thread;

        public int SampleCount { get; }

        public bool IsDisposed => EglContext.IsClosed;

        [Obsolete("The lifetime is managed by the app host")]
        public void Dispose()
        {
            // For compatibility with existing code
        }

        private void DoDispose()
        {
            using (EglContext.MakeCurrent(forRendering: false))
            {
                SkiaContext.Dispose();
                BackendContext.Dispose();
            }
            EglContext.Dispose();
        }

        public EglContext.Scope MakeCurrent(bool forRendering, EglSurface surface = null)
        {
            if (!IsOnCorrectThread)
                throw new InvalidOperationException("MakeCurrent called on the wrong thrad");

            return EglContext.MakeCurrent(forRendering, surface);
        }

        [Obsolete("Use MakeCurrent(bool forRendering, EglSurface surface) instead")]
        public void MakeCurrent(EglSurface surface = null)
        {
            if (!IsOnCorrectThread)
                throw new InvalidOperationException("MakeCurrent called on the wrong thrad");

            if (!NativeEgl.eglMakeCurrent(EglContext.Display, surface, surface, EglContext))
            {
                throw new Exception("Failed to make EGLSurface current");
            }
        }
    }
}
