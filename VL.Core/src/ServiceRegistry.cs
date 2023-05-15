#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;

namespace VL.Core
{
    public class ServiceRegistry : IServiceProvider
    {
        private static ServiceRegistry? global;

        /// <summary>
        /// The service registry for the current thread. Throws <see cref="InvalidOperationException"/> in case no registry is installed.
        /// </summary>
        [Obsolete("Use IAppHost.Current.Services", error: false)]
        public static ServiceRegistry Current => IAppHost.Current.Services;

        /// <summary>
        /// The service registry for the current thread or the global one if there's no registry installed on the current thread.
        /// </summary>
        [Obsolete("Use IAppHost.CurrentOrGlobal.Services", error: true)]
        public static ServiceRegistry CurrentOrGlobal => IAppHost.CurrentOrGlobal.Services;

        /// <summary>
        /// The service registry for the whole application.
        /// </summary>
        [Obsolete("Use IAppHost.Global.Services", error: true)]
        public static ServiceRegistry Global => global!;

        /// <summary>
        /// Whether or not a context is installed on the current thread.
        /// </summary>
        [Obsolete("Use IAppHost.IsCurrent()", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool IsCurrent() => IAppHost.IsCurrent();

        private readonly ConcurrentDictionary<Type, Registration> registrations = new();
        private readonly IServiceProvider? parent;

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
            if (service is null)
                throw new ArgumentNullException(nameof(service));

            registrations[typeof(T)] = new Registration(new Lazy<object>(() => service, isThreadSafe: false));
            return this;
        }

        public ServiceRegistry RegisterServiceLazy<T>(Func<T> serviceFactory)
        {
            if (serviceFactory is null)
                throw new ArgumentNullException(nameof(serviceFactory));

            registrations[typeof(T)] = new Registration(
                LazyService: new Lazy<object>(
                    valueFactory: () =>
                    {
                        var service = serviceFactory();
                        if (service is null)
                            throw new Exception($"The service factory for {typeof(T)} returned null.");
                        return service;
                    }, 
                    isThreadSafe: true));

            return this;
        }

        public object? GetService(Type serviceType)
        {
            return registrations.ValueOrDefault(serviceType).LazyService?.Value ?? parent?.GetService(serviceType);
        }

        record struct Registration(Lazy<object> LazyService);
    }

    public static class ServiceProviderExtensions
    {
        public static T? GetService<T>(this IServiceProvider serviceProvider) where T : class => serviceProvider.GetService(typeof(T)) as T;

        public static T GetOrAddService<T>(this ServiceRegistry serviceRegistry, Func<T> factory) where T : class
        {
            lock (serviceRegistry)
            {
                var service = serviceRegistry.GetService<T>();
                if (service is null)
                    serviceRegistry.RegisterService(service = factory());
                return service;
            }
        }

        public static ServiceRegistry EnsureService<TService>(this ServiceRegistry serviceRegistry, Func<TService> factory) where TService : class
        {
            var service = serviceRegistry.GetService<TService>();
            if (service is null)
                serviceRegistry.RegisterServiceLazy(factory);
            return serviceRegistry;
        }

        public static ServiceRegistry NewRegistry(this IServiceProvider parent) => new ServiceRegistry(parent);
    }
}
#nullable restore