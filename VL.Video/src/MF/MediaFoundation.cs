using System;
using System.Reactive.Disposables;
using System.Runtime.Versioning;
using System.Threading;

using static Windows.Win32.PInvoke;

namespace VL.Video.MF
{
    [SupportedOSPlatform("windows6.0.6000")]
    internal static class MediaFoundation
    {
        static int refCount;

        public static IDisposable Use()
        {
            if (Interlocked.Increment(ref refCount) == 1)
                MFStartup(MF_VERSION, 0).ThrowOnFailure();

            return Disposable.Create(Release);
        }

        private static void Release()
        {
            if (Interlocked.Decrement(ref refCount) == 0)
                MFShutdown().ThrowOnFailure();
        }
    }
}
