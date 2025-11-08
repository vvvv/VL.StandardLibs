#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D11;
using static VL.Skia.Egl.D3D11Utils;
using static VL.Skia.Egl.NativeEgl;
using static Windows.Win32.PInvoke;
using EGLConfig = System.IntPtr;
using EGLContext = System.IntPtr;
using EGLDisplay = System.IntPtr;
using EGLSurface = System.IntPtr;

namespace VL.Skia.Egl
{
    sealed class EglContextProvider : IDisposable
    {
        private readonly EglDisplayProvider displayProvider;
        private EglContext? context;
        public EglContextProvider(EglDisplayProvider displayProvider)
        {
            this.displayProvider = displayProvider;
        }
        public EglContext GetContext(int sampleCount = 0)
        {
            var display = displayProvider.GetDisplay();
            if (context?.Display != display)
            {
                context?.Dispose();
                context = null;
            }
            return context ??= EglContext.New(display, sampleCount);
        }
        public void Dispose()
        {
            context?.Dispose();
            context = null;
        }
    }

    public sealed unsafe class EglContext : EglResource
    {
        public static EglContext New(EglDisplay display, int sampleCount = 0, EglContext? shareContext = null)
        {
            int[] configAttributes = [
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

            // Try ES 3.1 first
            int[] contextAttributes32 =
            [
                EGL_CONTEXT_CLIENT_VERSION, 3,
                EGL_CONTEXT_MINOR_VERSION_KHR, 1,  // Request ES 3.1
                EGL_NONE
            ];

            // Fallback to ES 3.0 if ES 3.1 not available
            int[] contextAttributes30 =
            [
                EGL_CONTEXT_CLIENT_VERSION, 3,
                EGL_NONE
            ];

            EGLDisplay[] configs = new EGLDisplay[1];
            if ((!eglChooseConfig(display, configAttributes, configs, configs.Length, out int numConfigs)) || (numConfigs == 0))
            {
                throw new EglException($"Failed to choose first EGLConfig.");
            }
            var config = configs[0];

            // Try ES 3.1 first
            var context = eglCreateContext(display, config, share_context: shareContext, contextAttributes32);
            if (context == default)
            {
                // Fall back to ES 3.0
                context = eglCreateContext(display, config, share_context: shareContext, contextAttributes30);
                if (context == default)
                {
                    throw new EglException($"Failed to create EGL context.");
                }
            }

            return new EglContext(display, context, config, shareContext, sampleCount);
        }

        private readonly EglDisplay display;
        private readonly EGLConfig config;
        private readonly EglContext? shareContext;

        private EglContext(EglDisplay display, EGLContext context, EGLConfig config, EglContext? shareContext, int sampleCount)
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

        [Obsolete("Use Display")]
        public EglDisplay Dislpay => display;

        public int SampleCount { get; }

        public EglContext? ShareContext => shareContext;

        public bool IsLost => display.IsLost;

        public EglSurface CreatePlatformWindowSurface(nint nativeWindow, bool directComposition = false)
        {
            if (nativeWindow == default)
                throw new ArgumentNullException(nameof(nativeWindow));

            var surfaceAttributes = GetAttributes().ToArray();
            var surface = eglCreatePlatformWindowSurface(display, config, nativeWindow, surfaceAttributes);
            if (surface == default)
                Throw($"Failed to create EGL surface.");

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
                Throw($"Failed to create EGL surface.");

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
                Throw($"Failed to create EGL surface.");

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
                Throw($"Failed to create EGL surface.");

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
                Throw($"Failed to create EGL surface.");
            }

            return new EglSurface(display, surface);
        }

        public EglImage CreateImageFromD3D11Texture(nint texture)
        {
            var image = eglCreateImageKHR(display, default, EGL_D3D11_TEXTURE_ANGLE, texture, null);
            if (image == default)
                Throw("Failed to create EGL image.");
            return new EglImage(display, image);
        }

        public unsafe Scope MakeCurrent(bool forRendering, EglSurface? surface = null)
        {
            var deviceContext = default(ComPtr<ID3D11DeviceContext1>);
            if (forRendering && OperatingSystem.IsWindowsVersionAtLeast(8) && display.TryGetD3D11DeviceInterface(out var d3dDevice))
            {
                deviceContext = GetD3D11DeviceContext1(d3dDevice);
                var deviceContextState = EnsureDeviceContextState(d3dDevice);
                return new Scope(this, surface, deviceContext, deviceContextState);
            }

            return new Scope(this, surface);
        }

        ID3DDeviceContextState* deviceContextState;

        [SupportedOSPlatform("Windows8.0")]
        private unsafe ID3DDeviceContextState* EnsureDeviceContextState(ID3D11Device* d3dDevice)
        {
            if (this.deviceContextState != null)
                return this.deviceContextState;

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
            return this.deviceContextState = deviceContextState;
        }

        [SupportedOSPlatform("Windows8.0")]
        internal unsafe DeviceContextScope SwitchDeviceContextState()
        {
            if (display.TryGetD3D11DeviceInterface(out var d3dDevice))
            {
                var deviceContext = GetD3D11DeviceContext1(d3dDevice);
                var deviceContextState = EnsureDeviceContextState(d3dDevice);
                return new DeviceContextScope(deviceContext, deviceContextState);
            }
            return default;
        }

        public void MakeCurrent(EglSurface? surface = null)
        {
            Interlocked.Exchange(ref s_currentContext, this);
            Interlocked.Exchange(ref s_currentSurface, surface);

            if (!eglMakeCurrent(Display, surface, surface, this))
            {
                Throw($"Failed to make EGLContext current.");
            }
        }

        [ThreadStatic]
        private static EglContext? s_currentContext;
        [ThreadStatic]
        private static EglSurface? s_currentSurface;

        public void ReleaseCurrent()
        {
            if (Interlocked.CompareExchange(ref s_currentContext, null, this) == this)
            {
                s_currentContext = null;
                s_currentSurface = null;
            }

            if (!eglMakeCurrent(display, EGL_NO_SURFACE, EGL_NO_SURFACE, EGL_NO_CONTEXT))
            {
                Throw($"Failed to release current context.");
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

            if (OperatingSystem.IsWindowsVersionAtLeast(8) && deviceContextState != default)
                deviceContextState->Release();

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
            private readonly EglContext? previousContext;
            private readonly EglSurface? previousSurface;

            internal Scope(EglContext eglContext, EglSurface? surface, ID3D11DeviceContext1* deviceContext = null, ID3DDeviceContextState* newState = null)
            {
                previousContext = Interlocked.Exchange(ref s_currentContext, eglContext);
                previousSurface = Interlocked.Exchange(ref s_currentSurface, surface);

                if (OperatingSystem.IsWindowsVersionAtLeast(8) && deviceContext != null)
                    deviceContextScope = new DeviceContextScope(deviceContext, newState);

                if (!eglMakeCurrent(eglContext.Display, surface, surface, eglContext))
                {
                    eglContext.Throw($"Failed to make EGLContext current.");
                }
            }

            public void Dispose()
            {
                // If there was a previous context, restore it, otherwise release the current context
                var currentContext = Interlocked.Exchange(ref s_currentContext, previousContext);
                var currentSurface = Interlocked.Exchange(ref s_currentSurface, previousSurface);
                try
                {
                    if (previousContext != null)
                    {
                        if (!eglMakeCurrent(previousContext.Display, previousSurface, previousSurface, previousContext))
                        {
                            previousContext.Throw($"Failed to restore previous EGLContext.");
                        }
                    }
                    else
                    {
                        Debug.Assert(currentContext != null);
                        if (!eglMakeCurrent(currentContext.Display, default, default, default))
                        {
                            currentContext.Throw($"Failed to release EGLContext.");
                        }
                    }
                }
                catch (EglContextLostException)
                {
                    // Yeah well
                }
                finally
                {
                    if (OperatingSystem.IsWindowsVersionAtLeast(8))
                        deviceContextScope.Dispose();
                }
            }
        }

        [DoesNotReturn]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Throw(string? message = null)
        {
            var errorCode = eglGetError();
            var errorText = GetErrorText(errorCode);
            var errorMessage = message != null ? $"{message} {errorText}" : errorText;
            if (errorCode == EGL_CONTEXT_LOST)
                throw new EglContextLostException(errorMessage);
            else
                throw new EglException(errorMessage);
        }
    }
}
