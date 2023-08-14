using SkiaSharp;
using System;
using VL.Skia.Egl;

namespace VL.Skia
{
    public sealed class RenderContext : RefCounted
    {
        public const int ResourceCacheLimit = 512 * 1024 * 1024;

        // Little helper class to be able to access thread local memory from different thread
        sealed class Ref<T>
        {
            public T Value;
        }

        [ThreadStatic]
        private static Ref<RenderContext> s_threadContext;

        /// <summary>
        /// Returns the render context for the current thread.
        /// </summary>
        /// <returns>The render context for the current thread.</returns>
        public static RenderContext ForCurrentThread()
        {
            // Keep a render context per thread
            var rootRef = s_threadContext ??= new Ref<RenderContext>();

            var context = rootRef.Value;
            if (context != null)
            {
                context.AddRef();
                return context;
            }

            context = New(EglDisplay.GetPlatformDefault(createNewDevice: true /* We need a device for each thread */), 0);

            // The context might get disposed in a different thread (happend on some preview windows of camera devices)
            // Therefor store the "reference" so we can set it to null once the ref count goes to zero
            context.ThreadLocalStorage = rootRef;

            return rootRef.Value = context;
        }

        public static RenderContext New(EglDevice device, int msaaSamples)
        {
            var display = EglDisplay.FromDevice(device);
            return New(display, msaaSamples);
        }

        public static RenderContext New(EglDisplay display, int msaaSamples)
        {
            var context = EglContext.New(display, msaaSamples);
            context.MakeCurrent(default);
            var backendContext = GRGlInterface.CreateAngle();
            if (backendContext is null)
                throw new Exception("Failed to create ANGLE backend context");
            var skiaContext = GRContext.CreateGl(backendContext);
            if (skiaContext is null)
                throw new Exception("Failed to create Skia backend graphics context");

            // 512MB instead of the default 96MB
            skiaContext.SetResourceCacheLimit(ResourceCacheLimit);
            return new RenderContext(context, backendContext, skiaContext);
        }

        public readonly EglContext EglContext;
        private readonly GRGlInterface BackendContext;
        public readonly GRContext SkiaContext;

        RenderContext(EglContext eglContext, GRGlInterface backendContext, GRContext skiaContext)
        {
            EglContext = eglContext ?? throw new ArgumentNullException(nameof(eglContext));
            BackendContext = backendContext ?? throw new ArgumentNullException(nameof(backendContext));
            SkiaContext = skiaContext ?? throw new ArgumentNullException(nameof(skiaContext));
        }

        ~RenderContext()
        {
            Destroy();
        }

        Ref<RenderContext> ThreadLocalStorage { get; set; }

        protected override void Destroy()
        {
            GC.SuppressFinalize(this);

            MakeCurrent();

            SkiaContext.Dispose();
            BackendContext.Dispose();
            EglContext.Dispose();

            // Set the reference to null so a new context can be created if needed
            if (ThreadLocalStorage != null)
                ThreadLocalStorage.Value = null;
        }

        public void MakeCurrent(EglSurface surface = default)
        {
            EglContext.MakeCurrent(surface);
        }
    }
}
