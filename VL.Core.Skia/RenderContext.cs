﻿using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using VL.Core;
using VL.Lib.Basics.Video;
using VL.Skia.Egl;

namespace VL.Skia
{
    public unsafe sealed class RenderContext : IDisposable
    {
        public const int ResourceCacheLimit = 512 * 1024 * 1024;

        /// <summary>
        /// Returns the render context for the current thread.
        /// </summary>
        /// <returns>The render context for the current thread.</returns>
        public static unsafe RenderContext ForCurrentThread()
        {
            var appHost = AppHost.CurrentOrGlobal;
            return appHost.Services.GetOrAddService(s =>
            {

                var display = EglDisplay.ForCurrentApp();
                return New(display, 0, useLinearColorspace: false);
            }, allowToAskParent: false /* Please don't */);
        }

        public static RenderContext New(EglDisplay display, int msaaSamples, bool useLinearColorspace)
        {
            var context = EglContext.New(display, msaaSamples);
            using var _ = context.MakeCurrent(forRendering: false);
            var backendContext = GRGlInterface.CreateAngle();
            if (backendContext is null)
                throw new Exception("Failed to create ANGLE backend context");
            var skiaContext = GRContext.CreateGl(backendContext);
            if (skiaContext is null)
                throw new Exception("Failed to create Skia backend graphics context");

            // 512MB instead of the default 96MB
            skiaContext.SetResourceCacheLimit(ResourceCacheLimit);
            return new RenderContext(context, backendContext, skiaContext, useLinearColorspace);
        }

        public readonly EglContext EglContext;
        public readonly GRContext SkiaContext;

        private readonly GRGlInterface BackendContext;
        private readonly Thread thread;

        RenderContext(EglContext eglContext, GRGlInterface backendContext, GRContext skiaContext, bool useLinearColorspace)
        {
            EglContext = eglContext ?? throw new ArgumentNullException(nameof(eglContext));
            BackendContext = backendContext ?? throw new ArgumentNullException(nameof(backendContext));
            SkiaContext = skiaContext ?? throw new ArgumentNullException(nameof(skiaContext));
            UseLinearColorspace = useLinearColorspace;
            thread = Thread.CurrentThread;
        }

        public bool UseLinearColorspace { get; }

        public bool NeedsContextSwitch { get; }

        public int MsaaSamples => EglContext.MsaaSamples;

        public void Dispose()
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
#if DEBUG
            CheckThreadAccess();
#endif

            return EglContext.MakeCurrent(forRendering, surface);
        }

        private void CheckThreadAccess()
        {
            if (Thread.CurrentThread != thread)
                throw new InvalidOperationException("MakeCurrent called on the wrong thrad");
        }
    }
}
