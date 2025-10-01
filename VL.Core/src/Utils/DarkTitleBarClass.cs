using System;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using VL.Lib.Reactive;

namespace VL.Core.Utils;

// Based on https://stackoverflow.com/questions/11862315/changing-the-color-of-the-title-bar-in-winform
public static class DarkTitleBarClass
{
    public static IChannel<bool> Enabled { get; } = Channel.Create(false);

    public static IDisposable Install(nint handle)
    {
        return Enabled
            .ObserveOn(SynchronizationContext.Current)
            .StartWith(Enabled.Value)
            .Subscribe(v => UseImmersiveDarkMode(handle, v));
    }

    [SupportedOSPlatform("windows7.0")]
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int attrValue, int attrSize);

    [SupportedOSPlatform("windows10.0.17763")]
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    [SupportedOSPlatform("windows10.0.18985")]
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    internal static bool UseImmersiveDarkMode(nint handle, bool enabled)
    {
        if (handle == nint.Zero)
            return false;

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            var attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18985))
            {
                attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
            }

            int useImmersiveDarkMode = enabled ? 1 : 0;
            return DwmSetWindowAttribute(handle, attribute, ref useImmersiveDarkMode, sizeof(int)) == 0;
        }

        return false;
    }
}

