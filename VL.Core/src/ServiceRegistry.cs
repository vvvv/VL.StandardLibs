using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace VL.Core
{
    /// <summary>
    /// Provides services for the whole application (<see cref="Global"/>) or to a specific call stack (<see cref="Current"/>).
    /// All entry points (runtime instances, editor extensions, exported apps) will make a registry current before calling into the patch (<see cref="MakeCurrent"/>).
    /// Patches will capture the current service registry and restore it in callbacks should no other registry be current (<see cref="MakeCurrentIfNone"/>).
    /// </summary>
    public class ServiceRegistry : IServiceProvider
    {
        private static ServiceRegistry global;

        [ThreadStatic]
        private static ServiceRegistry current;

        /// <summary>
        /// The service registry for the current thread. Throws <see cref="InvalidOperationException"/> in case no registry is installed.
        /// </summary>
        public static ServiceRegistry Current => current ?? throw new InvalidOperationException("No service registry is installed on the current thread.");

        /// <summary>
        /// The service registry for the current thread or the global one if there's no registry installed on the current thread.
        /// </summary>
        public static ServiceRegistry CurrentOrGlobal => current ?? global;

        /// <summary>
        /// The service registry for the whole application.
        /// </summary>
        public static ServiceRegistry Global => global;

        /// <summary>
        /// Whether or not a context is installed on the current thread.
        /// </summary>
        public static bool IsCurrent() => current != null;

        /// <summary>
        /// Make the registry current on the current thread.
        /// </summary>
        /// <returns>A subscription which will restore the previous registry on dispose.</returns>
        public CurrentSubscription MakeCurrent()
        {
            return new CurrentSubscription(this);
        }

        /// <summary>
        /// Make the registry current on the current thread if no registry is current yet.
        /// </summary>
        /// <returns>A subscription which will restore the previous registry on dispose.</returns>
        public CurrentSubscription MakeCurrentIfNone()
        {
            return new CurrentSubscription(current ?? this);
        }

        public readonly struct CurrentSubscription : IDisposable
        {
            readonly ServiceRegistry saved;

            internal CurrentSubscription(ServiceRegistry context)
            {
                saved = current;
                current = context;
            }

            public void Dispose()
            {
                current = saved;
            }
        }

        private readonly ConcurrentDictionary<Type, object> services = new ConcurrentDictionary<Type, object>();
        private readonly IServiceProvider parent;

        public ServiceRegistry()
        {
            global = this;
        }

        // Called by unit tests
        internal void Unset()
        {
            global = null;
        }

        public ServiceRegistry(IServiceProvider parent)
        {
            this.parent = parent;
        }

        public ServiceRegistry RegisterService<T>(T service)
        {
            services[typeof(T)] = service;
            return this;
        }

        public object GetService(Type serviceType)
        {
            return services.ValueOrDefault(serviceType) ?? parent?.GetService(serviceType);
        }
    }

    public static class ServiceProviderExtensions
    {
        public static T GetService<T>(this IServiceProvider serviceProvider) where T : class => serviceProvider.GetService(typeof(T)) as T;

        public static T GetOrAddService<T>(this ServiceRegistry serviceRegistry, Func<T> factory, bool manageLifetime) where T : class
        {
            lock (serviceRegistry)
            {
                var service = serviceRegistry.GetService<T>();
                if (service is null)
                    serviceRegistry.RegisterService(service = factory());
                if (manageLifetime && service is IDisposable disposableService)
                    serviceRegistry.GetService<CompositeDisposable>().Add(disposableService);
                return service;
            }
        }

        [Obsolete("Use the overload where you need to commit to a lifetime")]
        public static T GetOrAddService<T>(this ServiceRegistry serviceRegistry, Func<T> factory) where T : class
        {
            // up to now lifetime wasn't managed
            return serviceRegistry.GetOrAddService(factory, manageLifetime: false);
        }

        public static ServiceRegistry EnsureService<TService>(this ServiceRegistry serviceRegistry, Func<TService> factory) where TService : class
        {            
            // up to now lifetime wasn't managed
            serviceRegistry.GetOrAddService(factory, manageLifetime: false);
            return serviceRegistry;
        }

        public static ServiceRegistry NewRegistry(this IServiceProvider parent) => new ServiceRegistry(parent);
    }
}
