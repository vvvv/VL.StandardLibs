using System;

using EGLDisplay = System.IntPtr;
using EGLContext = System.IntPtr;
using EGLConfig = System.IntPtr;
using EGLSurface = System.IntPtr;
using System.Collections.Generic;
using System.Linq;

namespace VL.Skia.Egl
{
    public sealed class EglContext : EglResource
    {
        private static readonly object contextCreationLock = new object();

        public static EglContext New(EglDisplay display, int msaaSamples)
        {
            lock (contextCreationLock)
            {
                int[] configAttributes = new[]
                {
                    NativeEgl.EGL_RED_SIZE, 8,
                    NativeEgl.EGL_GREEN_SIZE, 8,
                    NativeEgl.EGL_BLUE_SIZE, 8,
                    NativeEgl.EGL_ALPHA_SIZE, 8,
                    NativeEgl.EGL_DEPTH_SIZE, 8,
                    NativeEgl.EGL_STENCIL_SIZE, 8,
                    NativeEgl.EGL_SAMPLE_BUFFERS, msaaSamples > 0 ? 1 : 0,
                    NativeEgl.EGL_SAMPLES, msaaSamples,
                    NativeEgl.EGL_NONE
                };

                int[] contextAttributes = new[]
                {
                    NativeEgl.EGL_CONTEXT_CLIENT_VERSION, 3,
                    NativeEgl.EGL_NONE
                };

                EGLDisplay[] configs = new EGLDisplay[1];
                if ((NativeEgl.eglChooseConfig(display, configAttributes, configs, configs.Length, out int numConfigs) == NativeEgl.EGL_FALSE) || (numConfigs == 0))
                {
                    throw new Exception("Failed to choose first EGLConfig");
                }
                var config = configs[0];

                var context = NativeEgl.eglCreateContext(display, config, share_context: default, contextAttributes);
                if (context == default)
                {
                    throw new Exception("Failed to create EGL context");
                }

                return new EglContext(display, context, config, shareContext: null, msaaSamples);
            }
        }

        private readonly EglDisplay display;
        private readonly EGLConfig config;
        private readonly EglContext shareContext;
        private readonly int msaaSamples;

        private EglContext(EglDisplay display, EGLContext context, EGLConfig config, EglContext shareContext, int msaaSamples)
            : base(context)
        {
            this.display = display;
            this.config = config;
            this.shareContext = shareContext;
            this.msaaSamples = msaaSamples;
            shareContext?.AddRef();
        }

        public EglDisplay Dislpay => display;

        public EglSurface CreatePlatformWindowSurface(IntPtr nativeWindow, bool directComposition = false)
        {
            if (nativeWindow == default)
                throw new ArgumentNullException(nameof(nativeWindow));

            var surfaceAttributes = GetAttributes().ToArray();
            var surface = NativeEgl.eglCreatePlatformWindowSurface(display, config, nativeWindow, surfaceAttributes);
            if (surface == default)
                throw new Exception("Failed to create EGL surface");

            return new EglSurface(display, surface);

            IEnumerable<nint> GetAttributes()
            {
                if (directComposition)
                {
                    yield return NativeEgl.EGL_DIRECT_COMPOSITION_ANGLE;
                    yield return NativeEgl.EGL_TRUE;
                }
                yield return NativeEgl.EGL_NONE;
            }
        }

        public EglSurface CreateSurfaceFromClientBuffer(IntPtr buffer)
        {
            if (buffer == default)
                throw new ArgumentNullException(nameof(buffer));

            int[] surfaceAttributes = new[]
            {
                NativeEgl.EGL_NONE
            };

            var surface = NativeEgl.eglCreatePbufferFromClientBuffer(display, NativeEgl.EGL_D3D_TEXTURE_ANGLE, buffer, config, surfaceAttributes);
            if (surface == default)
                throw new Exception("Failed to create EGL surface");

            return new EglSurface(display, surface);
        }

        public EglSurface CreateSurfaceFromSharedHandle(int width, int height, IntPtr handle)
        {
            int[] surfaceAttributes = new[]
            {
                NativeEgl.EGL_WIDTH, width,
                NativeEgl.EGL_HEIGHT, height,
                NativeEgl.EGL_TEXTURE_TARGET, NativeEgl.EGL_TEXTURE_2D,
                NativeEgl.EGL_TEXTURE_FORMAT, NativeEgl.EGL_TEXTURE_RGBA,
                NativeEgl.EGL_NONE
            };

            EGLSurface surface = NativeEgl.eglCreatePbufferFromClientBuffer(display, NativeEgl.EGL_D3D_TEXTURE_2D_SHARE_HANDLE_ANGLE, handle, config, surfaceAttributes);
            if (surface == NativeEgl.EGL_NO_SURFACE)
            {
                throw new Exception("Failed to create EGL surface");
            }

            return new EglSurface(display, surface);
        }

        public EglImage CreateImageFromD3D11Texture(IntPtr texture)
        {
            var image = NativeEgl.eglCreateImageKHR(display, IntPtr.Zero, NativeEgl.EGL_D3D11_TEXTURE_ANGLE, texture, null);
            return new EglImage(display, image);
        }

        public void MakeCurrent(EglSurface eglSurface = default)
        {
            if (NativeEgl.eglMakeCurrent(display, eglSurface, eglSurface, this) == NativeEgl.EGL_FALSE)
            {
                throw new Exception("Failed to make EGLSurface current");
            }
        }

        public bool SwapInterval(int interval)
        {
            return (NativeEgl.eglSwapInterval(display, interval) == NativeEgl.EGL_TRUE);
        }

        public bool SwapBuffers(EGLSurface eglSurface)
        {
            return (NativeEgl.eglSwapBuffers(display, eglSurface) == NativeEgl.EGL_TRUE);
        }

        protected override void Destroy()
        {
            lock (contextCreationLock)
            {
                NativeEgl.eglDestroyContext(display, NativePointer);

                shareContext?.Release();

                display.Release();
            }
        }
    }
}
