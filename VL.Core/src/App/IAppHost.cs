using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace VL.Core
{
    public interface IAppHost
    {
        /// <summary>
        /// Raised when the application exits.
        /// </summary>
        IObservable<Unit> OnExit { get; }

        ConcurrentDictionary<Type, object> Components { get; } 
    }

    public static class AppHostHelpers
    {
        /// <summary>
        /// TODO: compare to service registry. Lazily built. App components get disposed on stop.
        /// </summary>
        public static void RegisterAppComponent<T>(this IVLFactory factory, Func<T> componentCtor)
        {
            factory.RegisterService(typeof(T), typeof(T), _ =>
            {
                var appHost = ServiceRegistry.Current.GetService<IAppHost>();
                return appHost.Components.GetOrAdd(typeof(T), t =>
                {
                    var component = componentCtor();
                    if (component is IDisposable d)
                        appHost.OnExit.Subscribe(_ => d.Dispose()); // might be cleaner to have this inside the IAppHost implementation
                    return component;
                });
            });
        }
    }
}
