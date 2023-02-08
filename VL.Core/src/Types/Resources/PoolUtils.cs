using System.Reactive.Disposables;

namespace VL.Lib.Basics.Resources
{
    internal static class PoolUtils
    {
        public static TPool Subscribe<TPool>(this IResourceProvider<TPool> poolProvider, SerialDisposable subscription)
        {
            var handle = poolProvider.GetHandle();
            subscription.Disposable = handle;
            return handle.Resource;
        }
    }
}
