#nullable enable
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading;
using VL.Core;
using VL.Lib.Animation;
using VL.Lib.Basics.Video;
using VL.Skia.Egl;

namespace VL.Skia
{
    public static class AppHostExtensions
    {
        public static RenderContextProvider GetRenderContextProvider(this AppHost appHost)
        {
            var threadLocal = appHost.GetThreadLocalRenderContextProvider();
            return threadLocal.Value!;
        }

        public static ThreadLocal<RenderContextProvider> GetThreadLocalRenderContextProvider(this AppHost appHost)
        {
            return appHost.Services.GetOrAddService(s =>
            {
                // Keep a render context provider per thread
                return new ThreadLocal<RenderContextProvider>(() =>
                {
                    // Access external device on main thread only
                    var isOnMainThread = SynchronizationContext.Current == appHost.SynchronizationContext;
                    var externalDeviceProvider = isOnMainThread ? s.GetService<IGraphicsDeviceProvider>() : null;
                    var deviceProvider = new EglDeviceProvider(externalDeviceProvider, appHost.DefaultLogger).DisposeBy(appHost);
                    var displayProvider = new EglDisplayProvider(deviceProvider).DisposeBy(appHost);
                    var eglContextProvider = new EglContextProvider(displayProvider).DisposeBy(appHost);
                    var renderContextProvider = new RenderContextProvider(eglContextProvider).DisposeBy(appHost);

                    if (isOnMainThread)
                    {
                        // Make sure the context is current whenever a new frame starts to ensure nodes that for example "download" pixel data (like Pipet) work.
                        var frameclock = s.GetService<IFrameClock>();
                        if (frameclock is not null)
                            frameclock.GetTicks().Subscribe(_ => renderContextProvider.GetRenderContext().MakeCurrent()).DisposeBy(appHost);
                    }

                    return renderContextProvider;
                });
            }, allowToAskParent: false);
        }
    }

    public sealed class RenderContextProvider : IDisposable
    {
        private readonly EglContextProvider eglContextProvider;
        private readonly Subject<Unit> onDeviceLost = new();
        private RenderContext? renderContext;

        internal RenderContextProvider(EglContextProvider eglContextProvider)
        {
            this.eglContextProvider = eglContextProvider;
        }

        public bool IsDisposed => onDeviceLost.IsDisposed;

        public IObservable<Unit> OnDeviceLost => onDeviceLost;

        public RenderContext GetRenderContext()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            var context = eglContextProvider.GetContext();
            if (renderContext != null && renderContext.EglContext != context)
            {
                onDeviceLost.OnNext(default);
                renderContext.DoDispose();
                renderContext = null;
            }

            return renderContext ??= CreateRenderContext(context);
        }

        private RenderContext CreateRenderContext(EglContext context)
        {
            var renderContext = RenderContext.New(context);
            // For compatibility with existing code
            renderContext.isManagedByAppHost = true;
            renderContext.MakeCurrent();
            return renderContext;
        }

        public void Dispose()
        {
            onDeviceLost.Dispose();
            renderContext?.DoDispose();
            renderContext = null;
        }
    }

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
            var renderContextProvider = appHost.GetRenderContextProvider();
            return renderContextProvider.GetRenderContext();
        }

        public static RenderContext New(EglContext context)
        {
            using var _ = context.MakeCurrent(forRendering: false);
            var backendContext = GRGlInterface.CreateAngle();
            if (backendContext is null)
                throw new Exception("Failed to create ANGLE backend context");
            var skiaContext = GRContext.CreateGl(backendContext);
            if (skiaContext is null)
                throw new Exception("Failed to create Skia backend graphics context");

            // 512MB instead of the default 96MB
            skiaContext.SetResourceCacheLimit(ResourceCacheLimit);
            return new RenderContext(context, backendContext, skiaContext, sampleCount: 1);
        }

        public readonly EglContext EglContext;
        public readonly GRContext SkiaContext;

        private readonly GRGlInterface BackendContext;
        private readonly Thread thread;
        internal bool isManagedByAppHost;

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

        public bool IsLost => EglContext.IsLost;

        public void Dispose()
        {
            if (isManagedByAppHost)
                return;

            if (IsDisposed)
                return;

            DoDispose();
        }

        internal void DoDispose()
        {
            SkiaContext.Dispose();
            BackendContext.Dispose();
            EglContext.Dispose();
        }

        public EglContext.Scope MakeCurrent(bool forRendering, EglSurface? surface = null)
        {
            if (!IsOnCorrectThread)
                throw new InvalidOperationException("MakeCurrent called on the wrong thrad");

            return EglContext.MakeCurrent(forRendering, surface);
        }

        public void MakeCurrent(EglSurface? surface = null)
        {
            if (!IsOnCorrectThread)
                throw new InvalidOperationException("MakeCurrent called on the wrong thrad");

            EglContext.MakeCurrent(surface);
        }
    }
}
