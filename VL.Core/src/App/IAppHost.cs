using System;
using System.Collections.Concurrent;
using System.Reactive;

namespace VL.Core
{
    public interface IAppHost
    {
        /// <summary>
        /// Raised when the application exits.
        /// </summary>
        IObservable<Unit> OnExit { get; }

        ConcurrentDictionary<Type, object> Components { get; }

        /// <summary>
        /// Add a component to your app. Its life time is now managed by the app lifetime.
        /// Do it in RegisterServices in order to make the service available as soon as possible for others to pick it up.
        /// Unlike service registry entries, app components get disposed on stop/close.
        /// Unlike using SingleInstancePerApp, app components can get accessed in stateless contexts.
        /// </summary>
        public static void RegisterAppComponent<T>(Func<T> componentCtor)
        {
            // we might get called by compiler and from runtime. let's check for apphost (runtime per entry point)
            var appHost = ServiceRegistry.Current.GetService<IAppHost>();
            var isAppContext = appHost != null;

            if (isAppContext)
                appHost.Components.GetOrAdd(typeof(T), t =>
                {
                    var x = componentCtor();

                    // we don't want disposal when apphost disposes, because it is long-living for an entry-point.
                    // we want disposal on Stop
                    if (x is IDisposable d)
                        appHost.OnExit.Subscribe(_ => 
                        {
                            d.Dispose();
                            appHost.Components.TryRemove(typeof(T), out var _);
                        });
                    return x;
                });
        }

        /// <summary>
        /// Retrieves the app component.
        /// </summary>
        public static T GetAppComponent<T>()
        {
            var appHost = ServiceRegistry.Current.GetService<IAppHost>();
            return (T)appHost.Components[typeof(T)];
        }
    }
}
