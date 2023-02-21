using SharpDX.MediaFoundation;
using System.Threading;

namespace VL.Video.MediaFoundation
{
    class MediaManagerService
    {
        static int refCount;

        public static void Initialize()
        {
            if (Interlocked.Increment(ref refCount) == 1)
                MediaManager.Startup();
        }

        public static void Release()
        {
            if (Interlocked.Decrement(ref refCount) == 0)
                MediaManager.Shutdown();
        }
    }
}
