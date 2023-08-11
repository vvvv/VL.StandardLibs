#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;

namespace VL.Core
{
    public class ServiceRegistry : IServiceProvider
    {
        /// <summary>
        /// The service registry for the current thread. Throws <see cref="InvalidOperationException"/> in case no registry is installed.
        /// </summary>
        [Obsolete("Use IAppHost.Current.Services", error: false)]
        public static ServiceRegistry Current => AppHost.Current.Services;

        /// <summary>
        /// The service registry for the current thread or the global one if there's no registry installed on the current thread.
        /// </summary>
        [Obsolete("Use IAppHost.CurrentOrGlobal.Services", error: true)]
        public static ServiceRegistry CurrentOrGlobal => AppHost.CurrentOrGlobal.Services;

        /// <summary>
        /// The service registry for the whole application.
        /// </summary>
        [Obsolete("Use IAppHost.Global.Services", error: true)]
        public static ServiceRegistry Global => AppHost.Global.Services;

        /// <summary>
        /// Whether or not a context is installed on the current thread.
        /// </summary>
        [Obsolete("Use IAppHost.IsCurrent()", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool IsCurrent() => AppHost.IsCurrent();

        private readonly ConcurrentDictionary<Type, Registration> registrations = new();
        private readonly IServiceProvider? parent;
        private readonly AppHost appHost;

        public ServiceRegistry(AppHost appHost, IServiceProvider? parent = null)
        {
            this.appHost = appHost;
            this.parent = parent;
        }

        /// <summary>
        /// Registers an already existing service of type T. The lifetime will not be managed by the host.
        /// </summary>
        public ServiceRegistry RegisterService<T>(T service)
        {
            if (service is null)
                throw new ArgumentNullException(nameof(service));

            registrations[typeof(T)] = new Registration(new Lazy<object>(() => service, isThreadSafe: false));
            return this;
        }

        /// <summary>
        /// Registers a service factory for the service of type T. The lifetime of the created instance will be managed by the host.
        /// </summary>
        public ServiceRegistry RegisterService<T>(Func<IServiceProvider, T> serviceFactory)
        {
            if (serviceFactory is null)
                throw new ArgumentNullException(nameof(serviceFactory));

            registrations[typeof(T)] = CreateRegistration(serviceFactory);

            return this;
        }

        public object? GetService(Type serviceType)
        {
            return registrations.ValueOrDefault(serviceType).LazyService?.Value ?? parent?.GetService(serviceType);
        }

        public T GetOrAddService<T>(Func<IServiceProvider, T> serviceFactory) where T : class
        {
            if (serviceFactory is null)
                throw new ArgumentNullException(nameof(serviceFactory));

            var registration = registrations.GetOrAdd(typeof(T), (type, factory) =>
            {
                var service = parent?.GetService(type);
                if (service != null)
                    return new Registration(new Lazy<object>(service));

                return CreateRegistration(factory);
            }, serviceFactory);

            return (T)registration.LazyService.Value;
        }

        private Registration CreateRegistration<T>(Func<IServiceProvider, T> serviceFactory)
        {
            return new Registration(
                LazyService: new Lazy<object>(
                    valueFactory: () =>
                    {
                        // Make sure the service gets built in the correct host
                        using (appHost.MakeCurrent())
                        {
                            var service = serviceFactory(this);
                            if (service is null)
                                throw new Exception($"The service factory for {typeof(T)} returned null.");
                            if (service is IDisposable disposable)
                                disposable.DisposeBy(appHost);
                            return service;
                        }
                    },
                    isThreadSafe: true));
        }

        record struct Registration(Lazy<object> LazyService);
    }

    public static class ServiceProviderExtensions
    {
        public static T? GetService<T>(this IServiceProvider serviceProvider) where T : class => serviceProvider.GetService(typeof(T)) as T;

        public static T GetRequiredService<T>(this IServiceProvider serviceProvider) where T : class => serviceProvider.GetService<T>() ?? throw new Exception($"The required service {typeof(T).FullName} was not found");
    }
}
#nullable restore