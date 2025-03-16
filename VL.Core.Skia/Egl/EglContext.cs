using System;

using EGLDisplay = System.IntPtr;
using EGLContext = System.IntPtr;
using EGLConfig = System.IntPtr;
using EGLSurface = System.IntPtr;
using System.Collections.Generic;
using System.Linq;
using static VL.Skia.Egl.NativeEgl;

namespace VL.Skia.Egl
{
    public sealed class EglContext : EglResource
    {
        public static EglContext New(EglDisplay display, int msaaSamples)
        {
            int[] configAttributes =
            [
                EGL_RED_SIZE, 8,
                EGL_GREEN_SIZE, 8,
                EGL_BLUE_SIZE, 8,
                EGL_ALPHA_SIZE, 8,
                EGL_DEPTH_SIZE, 8,
                EGL_STENCIL_SIZE, 8,
                EGL_SAMPLE_BUFFERS, msaaSamples > 0 ? 1 : 0,
                EGL_SAMPLES, msaaSamples,
                EGL_NONE
            ];

            int[] contextAttributes =
            [
                EGL_CONTEXT_CLIENT_VERSION, 3,
                EGL_NONE
            ];

            EGLDisplay[] configs = new EGLDisplay[1];
            if ((eglChooseConfig(display, configAttributes, configs, configs.Length, out int numConfigs) == EGL_FALSE) || (numConfigs == 0))
            {
                throw new Exception($"Failed to choose first EGLConfig. {GetLastError()}");
            }
            var config = configs[0];

            var context = eglCreateContext(display, config, share_context: default, contextAttributes);
            if (context == default)
            {
                throw new Exception($"Failed to create EGL context. {GetLastError()}");
            }

            return new EglContext(display, context, config, shareContext: null, msaaSamples);
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
        }

        public EglDisplay Dislpay => display;

        public int MsaaSamples => msaaSamples;

        public EglSurface CreatePlatformWindowSurface(nint nativeWindow, bool directComposition = false)
        {
            if (nativeWindow == default)
                throw new ArgumentNullException(nameof(nativeWindow));

            var surfaceAttributes = GetAttributes().ToArray();
            var surface = eglCreatePlatformWindowSurface(display, config, nativeWindow, surfaceAttributes);
            if (surface == default)
                throw new Exception($"Failed to create EGL surface. {GetLastError()}");

            return new EglSurface(display, surface);

            IEnumerable<nint> GetAttributes()
            {
                if (directComposition)
                {
                    yield return EGL_DIRECT_COMPOSITION_ANGLE;
                    yield return EGL_TRUE;
                }
                yield return EGL_NONE;
            }
        }

        public EglSurface CreatePlatformWindowSurface(nint nativeWindow, int width, int height)
        {
            if (nativeWindow == default)
                throw new ArgumentNullException(nameof(nativeWindow));

            var surfaceAttributes = GetAttributes().ToArray();
            var surface = eglCreatePlatformWindowSurface(display, config, nativeWindow, surfaceAttributes);
            if (surface == default)
                throw new Exception($"Failed to create EGL surface. {GetLastError()}");

            return new EglSurface(display, surface);

            IEnumerable<nint> GetAttributes()
            {
                yield return EGL_FIXED_SIZE_ANGLE;
                yield return EGL_TRUE;
                yield return EGL_WIDTH;
                yield return width;
                yield return EGL_HEIGHT;
                yield return height;
                yield return EGL_NONE;
            }
        }

        public EglSurface CreatePbufferSurface(int width, int height)
        {
            int[] surfaceAttributes =
            [
                EGL_WIDTH,          width,
                EGL_HEIGHT,         height,
                EGL_TEXTURE_FORMAT, EGL_TEXTURE_RGBA,
                EGL_TEXTURE_TARGET, EGL_TEXTURE_2D,
                EGL_NONE,           EGL_NONE,
            ];

            var surface = eglCreatePbufferSurface(display, config, surfaceAttributes);
            if (surface == default)
                throw new Exception($"Failed to create EGL surface. {GetLastError()}");

            return new EglSurface(display, surface);
        }

        public EglSurface CreateSurfaceFromClientBuffer(nint buffer)
        {
            if (buffer == default)
                throw new ArgumentNullException(nameof(buffer));

            int[] surfaceAttributes =
            [
                EGL_NONE
            ];

            var surface = eglCreatePbufferFromClientBuffer(display, EGL_D3D_TEXTURE_ANGLE, buffer, config, surfaceAttributes);
            if (surface == default)
                throw new Exception($"Failed to create EGL surface. {GetLastError()}");

            return new EglSurface(display, surface);
        }

        public EglSurface CreateSurfaceFromSharedHandle(int width, int height, nint handle)
        {
            int[] surfaceAttributes =
            [
                EGL_WIDTH, width,
                EGL_HEIGHT, height,
                EGL_TEXTURE_TARGET, EGL_TEXTURE_2D,
                EGL_TEXTURE_FORMAT, EGL_TEXTURE_RGBA,
                EGL_NONE
            ];

            EGLSurface surface = eglCreatePbufferFromClientBuffer(display, EGL_D3D_TEXTURE_2D_SHARE_HANDLE_ANGLE, handle, config, surfaceAttributes);
            if (surface == EGL_NO_SURFACE)
            {
                throw new Exception($"Failed to create EGL surface. {GetLastError()}");
            }

            return new EglSurface(display, surface);
        }

        public EglImage CreateImageFromD3D11Texture(nint texture)
        {
            var image = eglCreateImageKHR(display, default, EGL_D3D11_TEXTURE_ANGLE, texture, null);
            return new EglImage(display, image);
        }

        public void MakeCurrent(EglSurface surface = default)
        {
            if (eglMakeCurrent(display, surface, surface, this) == EGL_FALSE)
            {
                throw new Exception($"Failed to make EGLSurface current. {GetLastError()}");
            }
        }

        public void ReleaseCurrent()
        {
            if (eglMakeCurrent(display, EGL_NO_SURFACE, EGL_NO_SURFACE, EGL_NO_CONTEXT) == EGL_FALSE)
            {
                throw new Exception($"Failed to release current context. {GetLastError()}");
            }
        }

        public bool SwapInterval(int interval)
        {
            return (eglSwapInterval(display, interval) == EGL_TRUE);
        }

        public bool SwapBuffers(EGLSurface eglSurface)
        {
            return (eglSwapBuffers(display, eglSurface) == EGL_TRUE);
        }

        protected override void Destroy(nint nativePointer)
        {
            ReleaseCurrent();

            eglDestroyContext(display, nativePointer);
        }
    }
}
