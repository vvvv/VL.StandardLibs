using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using VL.Core;
using VL.Lib.Animation;
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
        public static unsafe RenderContext ForCurrentApp() => ForApp(AppHost.CurrentOrGlobal);

        public static unsafe RenderContext ForApp(AppHost appHost)
        {
            return appHost.Services.GetOrAddService(s =>
            {
                var display = EglDisplay.ForApp(appHost);
                var renderContext = New(display);

                // For compatibility with existing code
                var lifetime = Disposable.Create(renderContext, ctx => ctx.DoDispose());
                lifetime.DisposeBy(appHost);

                // Make sure the context is current now and whenever a new frame starts to ensure nodes that for example "download" pixel data (like Pipet) work.
                renderContext.MakeCurrent();
                var frameclock = s.GetService<IFrameClock>();
                if (frameclock is not null)
                    frameclock.GetTicks().Subscribe(_ => renderContext.MakeCurrent()).DisposeBy(appHost);

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
        private readonly Subject<Unit> onDispose = new();

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

        public IObservable<Unit> OnDispose => onDispose;

        [Obsolete("The lifetime is managed by the app host")]
        public void Dispose()
        {
            // For compatibility with existing code
        }

        private void DoDispose()
        {
            using (EglContext.MakeCurrent(forRendering: false))
            {
                onDispose.OnNext(Unit.Default);
                onDispose.Dispose();

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

        public void MakeCurrent(EglSurface surface = null)
        {
            if (!IsOnCorrectThread)
                throw new InvalidOperationException("MakeCurrent called on the wrong thrad");

            EglContext.MakeCurrent(surface);
        }
    }
}
