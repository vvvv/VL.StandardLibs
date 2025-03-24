using System;

using EGLDisplay = System.IntPtr;
using EGLContext = System.IntPtr;
using EGLConfig = System.IntPtr;
using EGLSurface = System.IntPtr;
using System.Collections.Generic;
using System.Linq;
using static VL.Skia.Egl.NativeEgl;
using Windows.Win32.Graphics.Direct3D11;
using static Windows.Win32.PInvoke;
using static VL.Skia.Egl.D3D11Utils;
using Windows.Win32.Graphics.Direct3D;
using System.Runtime.InteropServices;

namespace VL.Skia.Egl
{
    public sealed class EglContext : EglResource
    {
        public static EglContext New(EglDisplay display, int sampleCount = 0, EglContext shareContext = null)
        {
            int[] configAttributes =
            [
                EGL_RED_SIZE, 8,
                EGL_GREEN_SIZE, 8,
                EGL_BLUE_SIZE, 8,
                EGL_ALPHA_SIZE, 8,
                EGL_DEPTH_SIZE, 8,
                EGL_STENCIL_SIZE, 8,
                EGL_SAMPLE_BUFFERS, sampleCount > 1 ? 1 : 0,
                EGL_SAMPLES, sampleCount,
                EGL_NONE
            ];

            int[] contextAttributes =
            [
                EGL_CONTEXT_CLIENT_VERSION, 3,
                EGL_NONE
            ];

            EGLDisplay[] configs = new EGLDisplay[1];
            if ((!eglChooseConfig(display, configAttributes, configs, configs.Length, out int numConfigs)) || (numConfigs == 0))
            {
                throw new Exception($"Failed to choose first EGLConfig. {GetLastError()}");
            }
            var config = configs[0];

            var context = eglCreateContext(display, config, share_context: shareContext, contextAttributes);
            if (context == default)
            {
                throw new Exception($"Failed to create EGL context. {GetLastError()}");
            }

            return new EglContext(display, context, config, shareContext, sampleCount);
        }

        private readonly EglDisplay display;
        private readonly EGLConfig config;
        private readonly EglContext shareContext;

        private EglContext(EglDisplay display, EGLContext context, EGLConfig config, EglContext shareContext, int sampleCount)
            : base(context)
        {
            this.display = display;
            this.config = config;
            this.shareContext = shareContext;
            SampleCount = sampleCount;

            // To ensure cleanup happens in the right order
            var success = false;
            display.DangerousAddRef(ref success);

            if (shareContext != null)
            {
                success = false;
                shareContext.DangerousAddRef(ref success);
            }
        }

        public EglDisplay Display => display;

        public int SampleCount { get; }

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

        public unsafe Scope MakeCurrent(bool forRendering, EglSurface surface = null)
        {
            var deviceContext = default(ComPtr<ID3D11DeviceContext1>);
            if (forRendering && OperatingSystem.IsWindowsVersionAtLeast(8) && display.TryGetD3D11Device(out var d3dDevice))
            {
                deviceContext = GetD3D11DeviceContext1(d3dDevice);
                if (deviceContextState == default)
                {
                    using var device = GetD3D11Device1(d3dDevice);
                    D3D_FEATURE_LEVEL chosenFeatureLevel;
                    ID3DDeviceContextState* deviceContextState;
                    device.Ptr->CreateDeviceContextState(
                        0,
                        [device.Ptr->GetFeatureLevel()],
                        D3D11_SDK_VERSION,
                        in ID3D11Device1.IID_Guid,
                        &chosenFeatureLevel,
                        &deviceContextState);
                    this.deviceContextState = (nint)deviceContextState;
                }
                return new Scope(this, surface, deviceContext, (ID3DDeviceContextState*)deviceContextState);
            }

            return new Scope(this, surface);
        }

        nint deviceContextState;

        public void ReleaseCurrent()
        {
            if (!eglMakeCurrent(display, EGL_NO_SURFACE, EGL_NO_SURFACE, EGL_NO_CONTEXT))
            {
                throw new Exception($"Failed to release current context. {GetLastError()}");
            }
        }

        public bool SwapInterval(int interval)
        {
            return eglSwapInterval(display, interval);
        }

        public bool SwapBuffers(EGLSurface eglSurface)
        {
            return eglSwapBuffers(display, eglSurface);
        }

        protected override bool ReleaseHandle()
        {
            ReleaseCurrent();

            if (deviceContextState != default)
                Marshal.Release(deviceContextState);

            try
            {
                return eglDestroyContext(display, handle);
            }
            finally
            {
                display.DangerousRelease();
                shareContext?.DangerousRelease();
            }
        }

        public unsafe ref struct Scope : IDisposable
        {
            private readonly DeviceContextScope deviceContextScope;
            private readonly nint display;
            private readonly nint context;
            private readonly nint read;
            private readonly nint draw;

            internal Scope(EglContext eglContext, EglSurface surface, ID3D11DeviceContext1* deviceContext = null, ID3DDeviceContextState* newState = null)
            {
                if (OperatingSystem.IsWindowsVersionAtLeast(8) && deviceContext != null)
                    deviceContextScope = new DeviceContextScope(deviceContext, newState);

                display = eglGetCurrentDisplay();
                context = eglGetCurrentContext();
                draw = eglGetCurrentSurface(EGL_DRAW);
                read = eglGetCurrentSurface(EGL_READ);
                if (!eglMakeCurrent(eglContext.Display, surface, surface, eglContext))
                {
                    throw new Exception($"Failed to make EGLContext current. {GetLastError()}");
                }
            }

            public void Dispose()
            {
                if (display != default && !eglMakeCurrent(display, draw, read, context))
                {
                    throw new Exception($"Failed to make EGLContext current. {GetLastError()}");
                }

                if (OperatingSystem.IsWindowsVersionAtLeast(8))
                    deviceContextScope.Dispose();
            }
        }
    }
}
