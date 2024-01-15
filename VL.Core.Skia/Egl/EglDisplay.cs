#nullable enable
using System;
using System.Collections.Generic;
using Windows.Win32;

using EGLDisplay = System.IntPtr;

namespace VL.Skia.Egl
{
    public sealed class EglDisplay : EglResource
    {
        private static readonly Dictionary<EGLDisplay, EglDisplay> s_Displays = new Dictionary<EGLDisplay, EglDisplay>();

        private readonly RefCounted? dependentResource;

        public static EglDisplay? FromDevice(EglDevice angleDevice)
        {
            var display = NativeEgl.eglGetPlatformDisplayEXT(NativeEgl.EGL_PLATFORM_DEVICE_EXT, angleDevice, null);
            if (display == default)
                throw new Exception("Failed to get EGL display from device");
            if (!TryInitialize(display, angleDevice, out var eglDisplay))
                throw new Exception("Failed to initialize EGL display from device");
            return eglDisplay;
        }

        public static EglDisplay GetPlatformDefault(bool createNewDevice)
        {
            int[] defaultDisplayAttributes = new[]
            {
				// These are the default display attributes, used to request ANGLE's D3D11 renderer.
				// eglInitialize will only succeed with these attributes if the hardware supports D3D11 Feature Level 10_0+.
				NativeEgl.EGL_PLATFORM_ANGLE_TYPE_ANGLE, NativeEgl.EGL_PLATFORM_ANGLE_TYPE_D3D11_ANGLE,

				// EGL_ANGLE_DISPLAY_ALLOW_RENDER_TO_BACK_BUFFER is an optimization that can have large performance benefits on mobile devices.
				// Its syntax is subject to change, though. Please update your Visual Studio templates if you experience compilation issues with it.
				NativeEgl.EGL_EXPERIMENTAL_PRESENT_PATH_ANGLE, NativeEgl.EGL_EXPERIMENTAL_PRESENT_PATH_FAST_ANGLE, 

				// EGL_PLATFORM_ANGLE_ENABLE_AUTOMATIC_TRIM_ANGLE is an option that enables ANGLE to automatically call 
				// the IDXGIDevice3::Trim method on behalf of the application when it gets suspended. 
				// Calling IDXGIDevice3::Trim when an application is suspended is a Windows Store application certification requirement.
				NativeEgl.EGL_PLATFORM_ANGLE_ENABLE_AUTOMATIC_TRIM_ANGLE, NativeEgl.EGL_TRUE,
                NativeEgl.EGL_NONE,
            };

            int[] fl9_3DisplayAttributes = new[]
            {
				// These can be used to request ANGLE's D3D11 renderer, with D3D11 Feature Level 9_3.
				// These attributes are used if the call to eglInitialize fails with the default display attributes.
				NativeEgl.EGL_PLATFORM_ANGLE_TYPE_ANGLE, NativeEgl.EGL_PLATFORM_ANGLE_TYPE_D3D11_ANGLE,
                NativeEgl.EGL_PLATFORM_ANGLE_MAX_VERSION_MAJOR_ANGLE, 9,
                NativeEgl.EGL_PLATFORM_ANGLE_MAX_VERSION_MINOR_ANGLE, 3,
                NativeEgl.EGL_EXPERIMENTAL_PRESENT_PATH_ANGLE, NativeEgl.EGL_EXPERIMENTAL_PRESENT_PATH_FAST_ANGLE,
                NativeEgl.EGL_PLATFORM_ANGLE_ENABLE_AUTOMATIC_TRIM_ANGLE, NativeEgl.EGL_TRUE,
                NativeEgl.EGL_NONE,
            };

            int[] warpDisplayAttributes = new[]
            {
				// These attributes can be used to request D3D11 WARP.
				// They are used if eglInitialize fails with both the default display attributes and the 9_3 display attributes.
				NativeEgl.EGL_PLATFORM_ANGLE_TYPE_ANGLE, NativeEgl.EGL_PLATFORM_ANGLE_TYPE_D3D11_ANGLE,
                NativeEgl.EGL_PLATFORM_ANGLE_DEVICE_TYPE_ANGLE, NativeEgl.EGL_PLATFORM_ANGLE_DEVICE_TYPE_D3D_WARP_ANGLE,
                NativeEgl.EGL_EXPERIMENTAL_PRESENT_PATH_ANGLE, NativeEgl.EGL_EXPERIMENTAL_PRESENT_PATH_FAST_ANGLE,
                NativeEgl.EGL_PLATFORM_ANGLE_ENABLE_AUTOMATIC_TRIM_ANGLE, NativeEgl.EGL_TRUE,
                NativeEgl.EGL_NONE,
            };

            //
            // To initialize the display, we make three sets of calls to eglGetPlatformDisplayEXT and eglInitialize, with varying 
            // parameters passed to eglGetPlatformDisplayEXT:
            // 1) The first calls uses "defaultDisplayAttributes" as a parameter. This corresponds to D3D11 Feature Level 10_0+.
            // 2) If eglInitialize fails for step 1 (e.g. because 10_0+ isn't supported by the default GPU), then we try again 
            //    using "fl9_3DisplayAttributes". This corresponds to D3D11 Feature Level 9_3.
            // 3) If eglInitialize fails for step 2 (e.g. because 9_3+ isn't supported by the default GPU), then we try again 
            //    using "warpDisplayAttributes".  This corresponds to D3D11 Feature Level 11_0 on WARP, a D3D11 software rasterizer.
            //
            return TryCreate(defaultDisplayAttributes)
                ?? TryCreate(fl9_3DisplayAttributes)
                ?? TryCreate(warpDisplayAttributes)
                ?? throw new Exception("Failed to initialize EGL");

            unsafe EglDisplay? TryCreate(int[] displayAttributes)
            {
                EGLDisplay display;

                if (createNewDevice && OperatingSystem.IsWindowsVersionAtLeast(5, 1, 2600))
                {
                    // Use a dummy window to create a device context
                    using var moduleHandle = PInvoke.GetModuleHandle(default(string));

                    var hwnd = PInvoke.CreateWindowEx(
                        Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_APPWINDOW,
                        "Static",
                        "EglDisplay",
                        Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_OVERLAPPED,
                        PInvoke.CW_USEDEFAULT,
                        PInvoke.CW_USEDEFAULT,
                        PInvoke.CW_USEDEFAULT,
                        PInvoke.CW_USEDEFAULT,
                        default,
                        default,
                        hInstance: moduleHandle,
                        default);

                    if (hwnd == default)
                        throw new Exception("Failed to create window");

                    try
                    {
                        var hdc = PInvoke.GetDC(hwnd);
                        if (hdc == default)
                            throw new Exception("Failed to retrieve HDC from window");

                        display = NativeEgl.eglGetPlatformDisplayEXT(NativeEgl.EGL_PLATFORM_ANGLE_ANGLE, hdc, displayAttributes);
                    }
                    finally
                    {
                        PInvoke.DestroyWindow(hwnd);
                    }
                }
                else
                {
                    display = NativeEgl.eglGetPlatformDisplayEXT(NativeEgl.EGL_PLATFORM_ANGLE_ANGLE, NativeEgl.EGL_DEFAULT_DISPLAY, displayAttributes);
                }

                if (display == default)
                    return null;

                if (TryInitialize(display, dependentResource: null /* We should probably wrap the HDC, but should only happen once per thread */, out var eglDisplay))
                    return eglDisplay;

                return null;
            }
        }

        private static bool TryInitialize(EGLDisplay nativePointer, RefCounted? dependentResource, out EglDisplay? display)
        {
            lock (s_Displays)
            {
                if (!s_Displays.TryGetValue(nativePointer, out display))
                {
                    if (NativeEgl.eglInitialize(nativePointer, out int major, out int minor) == NativeEgl.EGL_FALSE)
                        return false;

                    s_Displays.Add(nativePointer, display = new EglDisplay(nativePointer, dependentResource));
                }
                else
                {
                    display.AddRef();
                }
                return true;
            }
        }

        private EglDisplay(EGLDisplay nativePointer, RefCounted? dependentResource) : base(nativePointer)
        {
            dependentResource?.AddRef();
            this.dependentResource = dependentResource;
        }

        public bool TryGetD3D11Device(out IntPtr d3dDevice)
        {
            if (NativeEgl.eglQueryDisplayAttribEXT(NativePointer, NativeEgl.EGL_DEVICE_EXT, out var devicePtr) == NativeEgl.EGL_TRUE &&
                NativeEgl.eglQueryDeviceAttribEXT(devicePtr, NativeEgl.EGL_D3D11_DEVICE_ANGLE, out d3dDevice) == NativeEgl.EGL_TRUE)
            {
                return true;
            }
            else
            {
                d3dDevice = default;
                return false;
            }
        }

        public void BindTexImage(EglSurface surface)
        {
            if (NativeEgl.eglBindTexImage(NativePointer, surface, NativeEgl.EGL_BACK_BUFFER) == NativeEgl.EGL_FALSE)
                throw new Exception("Failed to bind texture image");
        }

        public void ReleaseTexImage(EglSurface surface)
        {
            if (NativeEgl.eglReleaseTexImage(NativePointer, surface, NativeEgl.EGL_BACK_BUFFER) == NativeEgl.EGL_FALSE)
                throw new Exception("Failed to release texture image");
        }

        protected override void Destroy()
        {
            lock (s_Displays)
            {
                s_Displays.Remove(NativePointer);
                NativeEgl.eglTerminate(NativePointer);
                dependentResource?.Release();
            }
        }
    }
}
