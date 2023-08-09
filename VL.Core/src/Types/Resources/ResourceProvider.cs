using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;

namespace VL.Lib.Basics.Resources
{
    /// <summary>
    /// Provides an IResourceHandle, which provides access to a Disposable resource.
    /// Consumers need to dispose these Handles.
    /// Implementations provide mechanisms for distributing and sharing Disposable resources.
    /// </summary>
    public interface IResourceProvider<out T>
    {
        IResourceHandle<T> GetHandle();
    }

    /// <summary>
    /// Is returned by IResourceProvider.GetHandle().
    /// Provides access to a Disposable Resource.
    /// </summary>
    public interface IResourceHandle<out T> : IDisposable
    {
        T Resource { get; }
    }

    /// <summary>
    /// A connectable resource provider only works after calling Connect. 
    /// Disconnect via the disposable returned by Connect(). Only then the upstream handle gets disposed of.
    /// Used to share Resources more efficiently while connected.
    /// </summary>
    public interface IConnectableResourceProvider<out T> : IResourceProvider<T>
    {
        IDisposable Connect();
    }





    public static class ResourceProvider
    {
        public static class Default<T>
        {
            static IResourceProvider<T> FInstance;

            public static IResourceProvider<T> GetInstance(T resource) => FInstance ?? (FInstance = Return(resource));
        }

        /// <summary>
        /// Manages the lifetime of a resource.
        /// Every consumer will get its own handle asking the factory for a new resource.
        /// Disposing a handle will dispose the handle's resource.
        /// difference to proto: GetHandle() is not lazy, but will create the resource.
        /// </summary>
        public static IResourceProvider<T> New<T>(Func<T> factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return new Provider<T>(
                producer: () =>
                {
                    T resource = factory();
                    return new Handle<T>(
                        resource: resource,
                        disposeAction: () => (resource as IDisposable)?.Dispose()
                    );
                });
        }

        public static IResourceProvider<T> Defer<T>(Func<IResourceProvider<T>> factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            var upstream = new Lazy<IResourceProvider<T>>(factory, isThreadSafe: true);
            return new Provider<T>(() => upstream.Value.GetHandle());
        }

        /// <summary>
        /// Manages the lifetime of a resource from a pool. Same key will return a handle to the exact same resource.
        /// First registered factory method wins, but will be removed on disposal of the pooled resource.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="T">Type of the resource</typeparam>
        /// <param name="pool">The pool to use</param>
        /// <param name="key">The key for the pool and resource creation</param>
        /// <param name="factory">Factory method to create the resource provider from the key</param>
        /// <param name="delayDisposalInMilliseconds">The disposal delay in milliseconds after the last consumer has released its resource handle</param>
        /// <returns></returns>
        internal static IResourceProvider<T> NewPooledInternal<TKey, T>(
            Dictionary<TKey, IResourceProvider<T>> pool, 
            TKey key, 
            Func<TKey, IResourceProvider<T>> factory, 
            int delayDisposalInMilliseconds = 0)
        {
            if (pool is null) throw new ArgumentNullException(nameof(pool));
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return new Provider<T>(
                producer: () =>
                {
                    IResourceProvider<T> refCountedProvider;
                    lock (pool)
                    {
                        if (!pool.TryGetValue(key, out refCountedProvider))
                        {
                            refCountedProvider = factory(key)
                                .Finally(_ => { lock (pool) { pool.Remove(key); } })
                                .Publish()
                                .RefCount(delayDisposalInMilliseconds);
                            pool.Add(key, refCountedProvider);
                        }
                    }
                    return refCountedProvider.GetHandle();
                });
        }

        /// <summary>
        /// Manages the lifetime of a resource from a pool. Same key will return a handle to the exact same resource.
        /// First registered factory method wins, but will be removed on disposal of the pooled resource.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="T">Type of the resource</typeparam>
        /// <param name="nodeContext">Unused parameter which was fed by VL. Used to figure out which app this node is in.</param>
        /// <param name="key">The key for the pool and resource creation</param>
        /// <param name="factory">Factory method to create the resource provider from the key</param>
        /// <param name="delayDisposalInMilliseconds">The disposal delay in milliseconds after the last consumer has released its resource handle</param>
        /// <returns></returns>
        [Obsolete("Use the overload without the node context")]
        public static IResourceProvider<T> NewPooledPerApp<TKey, T>(NodeContext nodeContext, TKey key, Func<TKey, IResourceProvider<T>> factory, int delayDisposalInMilliseconds = 0)
            => NewPooledInternal(AppWideProviderPool<TKey, T>.GetPool(), key, factory, delayDisposalInMilliseconds);

        /// <summary>
        /// Manages the lifetime of a resource from a pool. Same key will return a handle to the exact same resource.
        /// First registered factory method wins, but will be removed on disposal of the pooled resource.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="T">Type of the resource</typeparam>
        /// <param name="nodeContext">Unused parameter which was fed by VL. Used to figure out which app this node is in.</param>
        /// <param name="key">The key for the pool and resource creation</param>
        /// <param name="factory">Factory method to create the resource provider from the key</param>
        /// <param name="delayDisposalInMilliseconds">The disposal delay in milliseconds after the last consumer has released its resource handle</param>
        /// <returns></returns>
        [Obsolete("Use the overload without the node context")]
        public static IResourceProvider<T> NewPooledPerApp<TKey, T>(NodeContext nodeContext, TKey key, Func<TKey, T> factory, int delayDisposalInMilliseconds = 0)
            => NewPooledPerApp(nodeContext, key, k => New(() => factory(k)), delayDisposalInMilliseconds);

        /// <summary>
        /// Manages the lifetime of a resource from a pool. Same key will return a handle to the exact same resource.
        /// First registered factory method wins, but will be removed on disposal of the pooled resource.
        /// </summary>
        /// <typeparam name="T">Type of the resource</typeparam>
        /// <param name="nodeContext">Unused parameter which was fed by VL. Used to figure out which app this node is in.</param>
        /// <param name="factory">Factory method to create the resource provider from the key</param>
        /// <param name="delayDisposalInMilliseconds">The disposal delay in milliseconds after the last consumer has released its resource handle</param>
        /// <returns></returns>
        [Obsolete("Use the overload without the node context")]
        public static IResourceProvider<T> NewPooledPerApp<T>(NodeContext nodeContext, Func<IResourceProvider<T>> factory, int delayDisposalInMilliseconds = 0)
            => NewPooledInternal(AppWideProviderPool<ValueTuple, T>.GetPool(), default, _ => factory(), delayDisposalInMilliseconds);

        /// <summary>
        /// Manages the lifetime of a resource from a pool. Same key will return a handle to the exact same resource.
        /// First registered factory method wins, but will be removed on disposal of the pooled resource.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="T">Type of the resource</typeparam>
        /// <param name="key">The key for the pool and resource creation</param>
        /// <param name="factory">Factory method to create the resource provider from the key</param>
        /// <param name="delayDisposalInMilliseconds">The disposal delay in milliseconds after the last consumer has released its resource handle</param>
        /// <returns></returns>
        public static IResourceProvider<T> NewPooledPerApp<TKey, T>(TKey key, Func<TKey, IResourceProvider<T>> factory, int delayDisposalInMilliseconds = 0)
            => NewPooledInternal(AppWideProviderPool<TKey, T>.GetPool(), key, factory, delayDisposalInMilliseconds);

        /// <summary>
        /// Manages the lifetime of a resource from a pool. Same key will return a handle to the exact same resource.
        /// First registered factory method wins, but will be removed on disposal of the pooled resource.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="T">Type of the resource</typeparam>
        /// <param name="key">The key for the pool and resource creation</param>
        /// <param name="factory">Factory method to create the resource provider from the key</param>
        /// <param name="delayDisposalInMilliseconds">The disposal delay in milliseconds after the last consumer has released its resource handle</param>
        /// <returns></returns>
        public static IResourceProvider<T> NewPooledPerApp<TKey, T>(TKey key, Func<TKey, T> factory, int delayDisposalInMilliseconds = 0)
            => NewPooledPerApp(key, k => New(() => factory(k)), delayDisposalInMilliseconds);

        /// <summary>
        /// Manages the lifetime of a resource from a pool. Same key will return a handle to the exact same resource.
        /// First registered factory method wins, but will be removed on disposal of the pooled resource.
        /// </summary>
        /// <typeparam name="T">Type of the resource</typeparam>
        /// <param name="factory">Factory method to create the resource provider from the key</param>
        /// <param name="delayDisposalInMilliseconds">The disposal delay in milliseconds after the last consumer has released its resource handle</param>
        /// <returns></returns>
        public static IResourceProvider<T> NewPooledPerApp<T>(Func<IResourceProvider<T>> factory, int delayDisposalInMilliseconds = 0)
            => NewPooledInternal(AppWideProviderPool<ValueTuple, T>.GetPool(), default, _ => factory(), delayDisposalInMilliseconds);

        /// <summary>
        /// Manages the lifetime of a resource from a pool. Same key will return a handle to the exact same resource.
        /// First registered factory method wins, but will be removed on disposal of the pooled resource.
        /// </summary>
        /// <typeparam name="T">Type of the resource</typeparam>
        /// <param name="nodeContext">Unused parameter which was fed by VL. Used to figure out which app this node is in.</param>
        /// <param name="factory">Factory method to create the resource provider from the key</param>
        /// <param name="delayDisposalInMilliseconds">The disposal delay in milliseconds after the last consumer has released its resource handle</param>
        /// <returns></returns>
        [Obsolete("Use the overload without the node context")]
        public static IResourceProvider<T> NewPooledPerApp<T>(NodeContext nodeContext, Func<T> factory, int delayDisposalInMilliseconds = 0)
            => NewPooledPerApp(nodeContext, () => New(() => factory()), delayDisposalInMilliseconds);

        /// <summary>
        /// Manages the lifetime of a resource from a pool. Same key will return a handle to the exact same resource.
        /// First registered factory method wins, but will be removed on disposal of the pooled resource.
        /// </summary>
        /// <typeparam name="T">Type of the resource</typeparam>
        /// <param name="factory">Factory method to create the resource provider from the key</param>
        /// <param name="delayDisposalInMilliseconds">The disposal delay in milliseconds after the last consumer has released its resource handle</param>
        /// <returns></returns>
        public static IResourceProvider<T> NewPooledPerApp<T>(Func<T> factory, int delayDisposalInMilliseconds = 0)
            => NewPooledPerApp(() => New(() => factory()), delayDisposalInMilliseconds);


        sealed class AppWideProviderPool<TKey, T>
        {
            private readonly Dictionary<TKey, IResourceProvider<T>> Pool = new Dictionary<TKey, IResourceProvider<T>>();

            public static Dictionary<TKey, IResourceProvider<T>> GetPool()
            {
                return AppHost.Current.Services.GetOrAddService(_ => new AppWideProviderPool<TKey, T>()).Pool;
            }
        }


        /// <summary>
        /// Manages the lifetime of a resource from a pool. Same key will return a handle to the exact same resource.
        /// First registered factory method wins, but will be removed on disposal of the pooled resource.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="T">Type of the resource</typeparam>
        /// <param name="key">The key for the pool and resource creation</param>
        /// <param name="factory">Factory method to create the resource provider from the key</param>
        /// <param name="delayDisposalInMilliseconds">The disposal delay in milliseconds after the last consumer has released its resource handle</param>
        /// <returns></returns>
        public static IResourceProvider<T> NewPooledSystemWide<TKey, T>(TKey key, Func<TKey, IResourceProvider<T>> factory, int delayDisposalInMilliseconds = 0)
           => NewPooledInternal(SystemWideProviderPool<TKey, T>.Pool, key, factory, delayDisposalInMilliseconds);


        /// <summary>
        /// Manages the lifetime of a resource from a pool. Same key will return a handle to the exact same resource.
        /// First registered factory method wins, but will be removed on disposal of the pooled resource.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="T">Type of the resource</typeparam>
        /// <param name="key">The key for the pool and resource creation</param>
        /// <param name="factory">Factory method to create the resource provider from the key</param>
        /// <param name="delayDisposalInMilliseconds">The disposal delay in milliseconds after the last consumer has released its resource handle</param>
        /// <returns></returns>
        public static IResourceProvider<T> NewPooledSystemWide<TKey, T>(TKey key, Func<TKey, T> factory, int delayDisposalInMilliseconds = 0)
           => NewPooledSystemWide(key, k => New(() => factory(k)), delayDisposalInMilliseconds);



        static class SystemWideProviderPool<TKey, T> 
        {
            public static readonly Dictionary<TKey, IResourceProvider<T>> Pool = new Dictionary<TKey, IResourceProvider<T>>();
        }

        public static bool ExistsInSystemWideProviderPool<TKey, T>(TKey key) 
        {
            return SystemWideProviderPool<TKey, T>.Pool.ContainsKey(key);
        }


        




        /// <summary>
        /// Manages the lifetime of a resource.
        /// Every consumer will get its own handle asking the factory for a new resource.
        /// Disposing a handle will dispose the handle's resource.
        /// difference to proto: GetHandle() is not lazy, but will create the resource.
        /// </summary>
        public static IResourceProvider<T> New<T>(Func<T> factory, Action<T> beforeDispose)
            => new Provider<T>(
                producer: () =>
                {
                    T resource = factory();
                    return new Handle<T>(
                        resource: resource,
                        disposeAction: () =>
                        {
                            beforeDispose(resource);
                            (resource as IDisposable)?.Dispose(); 
                        }
                    );
                });

        public static ResourceProviderMonitor<T> Monitor<T>(this IResourceProvider<T> source)
            => new ResourceProviderMonitor<T>(source);

        /// <summary>
        /// Will always provide same single resource. It exists already. 
        /// So its not the responsibility of Return() to dispose it.
        /// 
        /// Could also imagine a ReturnLazy that takes a Func&lt;TResource&gt;, 
        /// but as it is used mostly inside the monade it is already lazy to GetHandle() from downstream
        /// </summary>
        public static IResourceProvider<T> Return<T>(T singleItem)
            => new Provider<T>(
                producer: () =>
                {
                    // https://en.wikipedia.org/wiki/Monad_(functional_programming) See chapter Monad laws.
                    // return acts approximately as a neutral element of >>=, in that:

                    //   1)      (return x) >>= f   ≡   f x

                    //   2)      m >>= return       ≡   m

                    // we've got no reduceable expressions in a language as c#, so equivalence should be checked by tracking the order 
                    // of execution of all publicly available methods (provider and its handle) of a whole monadic composition.
                    // (e.g. "m >>= return" which translates to "src.Bind(Return)" is a composition). 
                    // with a primitive implementation like this it is easy to see the equivalence .. ≡ .. for both rules above.

                    return new Handle<T>(
                        resource: singleItem, 
                        disposeAction: () => { }
                        );
                });

        /// <summary>
        /// Will always provide the same single resource. It already exists.
        /// The resource gets reference counted, therefor the recommended usage pattern is the following:
        /// <code>
        ///   var provider = ResourceProvider.Return(..)
        ///   using (provider.GetHandle())
        ///   {
        ///     // Pass the provider to consumers
        ///   }
        /// </code>
        /// </summary>
        public static IResourceProvider<T> Return<T>(T singleItem, Action<T> disposeAction)
        {
            return Return(singleItem, singleItem, disposeAction);
        }

        /// <summary>
        /// Will always provide the same single resource. It already exists.
        /// The resource gets reference counted, therefor the recommended usage pattern is the following:
        /// <code>
        ///   var provider = ResourceProvider.Return(..)
        ///   using (provider.GetHandle())
        ///   {
        ///     // Pass the provider to consumers
        ///   }
        /// </code>
        /// </summary>
        public static IResourceProvider<T> Return<T, TState>(T singleItem, TState state, Action<TState> disposeAction)
        {
            var refCount = 0;
            return new Provider<T>(() =>
            {
                if (disposeAction is null)
                    throw new ObjectDisposedException(singleItem?.ToString());

                Interlocked.Increment(ref refCount);

                return new Handle<T, TState>(
                    resource: singleItem,
                    state: state,
                    disposeAction: s =>
                    {
                        if (Interlocked.Decrement(ref refCount) <= 0)
                        {
                            var action = Interlocked.Exchange(ref disposeAction, null);
                            action?.Invoke(s);
                        }
                    });
            });
        }

        /// <summary>
        /// SelectMany
        /// Create a ResourceProvider per source resource. Creating any provider will work.
        /// Takes into account that the resulting resources may depend on the source resources.
        /// </summary>
        public static IResourceProvider<TOut> Bind<T, TOut>(this IResourceProvider<T> source, Func<T, IResourceProvider<TOut>> providerSelector)
        { 
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (providerSelector is null) throw new ArgumentNullException(nameof(providerSelector));

            return new Provider<TOut>(
                producer: () =>
                {
                    // the order of the following lines is clear as they depend on each other:
                    var upHandle = source.GetHandle();
                    var innerProvider = providerSelector(upHandle.Resource);
                    var innerHandle = innerProvider.GetHandle();

                    return new Handle<TOut>(
                        resource: innerHandle.Resource,
                        disposeAction: () =>
                        {
                            //Binding two functions in succession is the same as binding one function that can be determined from them:
                            //(m >>= f) >>= g ≡ m >>= ( \x-> (f x >>= g) )

                            // what happens when diposing a "mfg" handle?

                            innerHandle.Dispose();   // LH case: gH.Dispose();                   // RH case: gH.Dispose(); fH.Dispose();
                            upHandle.Dispose();      // gfm      fH.Dispose(); mH.Dispose();     // gfm      mH.Dispose();

                            // it feels hard to ever get it wrong?!
                            //upHandle.Dispose();      // LH case: mH.Dispose(); fH.Dispose();     // RH case: mH.Dispose();
                            //innerHandle.Dispose();   // mfg      gH.Dispose();                   // mfg      fH.Dispose(); gH.Dispose(); 

                            //still it feels right to dispose the inner dependant resource first. 
                        });
                });
        }

        /// <summary>
        /// Provides a resource for every sink, asking for a source resource every time a sink resource is demanded. No resources are shared hereby.
        /// The user provided resource will not get managed as it may exist already. (Select(form => form.Controls[0]) should not dispose the control)
        /// If you create a new resource that you want to get managed use BindNew for this.
        /// </summary>
        public static IResourceProvider<TOut> Bind<T, TOut>(this IResourceProvider<T> source, Func<T, TOut> select)
               => source.Bind(sourceResource => Return(select(sourceResource)));

        /// <summary>
        /// Provides a new resource for every sink, asking for a source resource every time a sink resource is demanded. No resources are shared hereby.
        /// The user provided resource will get managed.
        /// </summary>
        public static IResourceProvider<TOut> BindNew<T, TOut>(this IResourceProvider<T> source, Func<T, TOut> select)
                => source.Bind(sourceResource => New(() => select(sourceResource)));


        /// <summary>
        /// Applies an action on a resource and outputs the same resource again.
        /// </summary>
        public static IResourceProvider<T> Do<T>(this IResourceProvider<T> source, Action<T> action)
                => source.Bind(sourceResource => { action(sourceResource); return Return(sourceResource); });

        /// <summary>
        /// Just doesn't let you access a resource that doesn't match your needs. Gives you default instead.
        /// </summary>
        public static IResourceProvider<T> Where<T>(this IResourceProvider<T> source, Func<T, bool> predicate)
            => source.Bind(sourceResource => Return(predicate(sourceResource) ? sourceResource : default));

        /// <summary>
        /// Lets you connect and disconnect manually to the source.
        /// All provided handles will now get access to the same upstream resource. 
        /// You may disconnect and reconnect to provide access to a new upstream resource.
        /// Former disposable provider Memoize
        /// </summary>
        public static IConnectableResourceProvider<T> Publish<T>(this IResourceProvider<T> source, Action<T> resetResource = null)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            object l = new object();
            IResourceHandle<T> upHandle = null;
            bool firstUse = true;

            return new ConnectableProvider<T>(
                producer: () =>
                {
                    lock (l)
                    {
                        T resource = default;
                        if (upHandle != null)
                        {                             
                            // fetching the resource when creating the downstream handle. 
                            // this handle will always provide the same resource.
                            resource = upHandle.Resource;
                            if (!firstUse)
                                resetResource?.Invoke(resource); // reusing resource
                        }
                        firstUse = false;
                        
                        return new Handle<T>(
                            resource: resource, 
                            disposeAction: () => { }); 
                    }
                },
                connectAction: () =>
                {
                    lock (l)
                    {
                        if (upHandle != null)
                            throw new InvalidOperationException("Connection already established");
                        upHandle = source.GetHandle();
                        firstUse = true;
                        return () => // Disconnect Action
                        {
                            lock (l)
                            {
                                upHandle.Dispose();
                                upHandle = null;
                            } 
                        };
                    }
                });
        }

        /// <summary>
        /// Lets you connect and disconnect manually to the source.
        /// Manages a pool of handles from the upstream provider.
        /// On GetHandle, this will return a Handle containing a resource that is not currently in use.
        /// Manages a pool of upstream handles. When a downstream handle gets disposed, it's inner upstream Handle will be put back into the pool.
        /// 
        /// Will dispose every upstream handle still in the pool on disconnect.
        /// Former disposable provider Pool
        /// </summary>
        static IConnectableResourceProvider<T> PublishPooled<T>(this IResourceProvider<T> provider, Action<T> resetResource = null)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));

            object l = new object();
            Stack<IResourceHandle<T>> FPool = null;

            return new ConnectableProvider<T>(
                producer: () =>
                {
                    lock (l)
                    {
                        if (FPool == null)
                            return new Handle<T>(
                                resource: default, 
                                disposeAction: () => { });
                        else
                        {
                            IResourceHandle<T> handle;
                            if (FPool.Count > 0)
                            {
                                handle = FPool.Pop();
                                resetResource?.Invoke(handle.Resource);
                            }
                            else
                                handle = provider.GetHandle();

                            return new Handle<T>(
                                resource: handle.Resource,
                                disposeAction: () => {
                                    lock (l)
                                    {
                                        // disconnection and even reconnection might have happened in between. 
                                        // it doesn't matter. if currently there is no pool we have to take care of disposing the handle. 
                                        // if there is a pool, we can hand over the handle for the pool to dispose it on disconnection.
                                        if (FPool == null)
                                            handle.Dispose();
                                        else
                                        {
                                            FPool.Push(handle);
                                        }
                                    }
                                });
                        }
                    }
                },
                connectAction: () =>
                {
                    lock (l)
                    {
                        if (FPool != null)
                            throw new InvalidOperationException("Connection already established");
                        FPool = new Stack<IResourceHandle<T>>();
                       
                        return () => // Disconnect Action
                        {
                            lock (l)
                            {
                                foreach (var handle in FPool)
                                    handle.Dispose();
                                FPool = null;
                            }
                        };
                    }
                });
        }

        /// <summary>
        /// Connects to upstream IConnectableResourceProvider when first handle is requested
        /// Will maintain connection until no handle is active any more
        /// After delayDisposalInMilliseconds, will disconnect from upstream IConnectableResourceProvider
        /// </summary>
        static IResourceProvider<T> RefCount<T>(this IConnectableResourceProvider<T> source, int delayDisposalInMilliseconds)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            if (delayDisposalInMilliseconds > 0)
                return source.RefCount(Observable.Timer(TimeSpan.FromMilliseconds(delayDisposalInMilliseconds)));
            else
                return source.RefCount<T,int>();
        }

        /// <summary>
        /// Connects to upstream IConnectableResourceProvider when first handle is requested
        /// Will maintain connection until no handle is active any more
        /// After disposalTriggerSource fired, will disconnect from upstream IConnectableResourceProvider
        /// </summary>
        static IResourceProvider<T> RefCount<T,U>(this IConnectableResourceProvider<T> source, IObservable<U> disposalTriggerSource = null)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            int i = 0;
            object l = new object();
            var connectionSubscription = new SerialDisposable();
            var timerSubscription = new SerialDisposable();
            var context = default(SynchronizationContext);
            var disposalTriggerSourceInitialized = false;

            return new Provider<T>(
                producer: () =>
                {
                    IResourceHandle<T> handle;
                    lock (l)
                    {
                        // Retrieve the sync context
                        context = SynchronizationContext.Current;
                        // Cancel a running disposal
                        timerSubscription.Disposable = null;
                        if (i == 0 && connectionSubscription.Disposable == null)
                            connectionSubscription.Disposable = source.Connect();
                        i++;
                        handle = source.GetHandle();
                    }
                    return new Handle<T>(
                        resource: handle.Resource, 
                        disposeAction: () =>
                        {
                            lock (l)
                            {
                                handle.Dispose();

                                i--;
                                if (i == 0)
                                {
                                    if (disposalTriggerSource != null)
                                    {
                                        // Schedule a disposal
                                        if (!disposalTriggerSourceInitialized)
                                        {
                                            disposalTriggerSourceInitialized = true;
                                            // We're only interested in one event
                                            disposalTriggerSource = disposalTriggerSource.Take(1);
                                            // Ensure on application exit we're triggered as well
                                            var appHost = AppHost.CurrentOrGlobal;
                                            if (appHost != null)
                                                disposalTriggerSource = Observable.Merge(disposalTriggerSource, appHost.OnExit.Select(_ => default(U)));
                                            // If a sync context is set make sure it's on that one
                                            if (context != null)
                                                disposalTriggerSource = disposalTriggerSource.ObserveOn(context);
                                        }
                                        timerSubscription.Disposable = disposalTriggerSource.Subscribe(_ =>
                                        {
                                            lock (l)
                                            {
                                                if (i == 0)
                                                    connectionSubscription.Disposable = null;
                                            }
                                        });
                                    }
                                    else
                                        connectionSubscription.Disposable = null;
                                }
                            }
                        });
                });
        }

        /// <summary>
        /// Handles get handed out in a serial fashion. Only one handle is in circulation at a given point in time.
        /// </summary>
        static IResourceProvider<T> Serialize<T>(this IResourceProvider<T> source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            SemaphoreSlim mutex = null;
            int waiting = 0;
            object l = new object();
            return new Provider<T>(
                producer: () =>
                {
                    lock (l)
                    {
                        if (waiting == 0)
                            mutex = new SemaphoreSlim(1, 1);
                        waiting++;
                    }
                    mutex.Wait();
                    var upHandle = source.GetHandle();
                    return new Handle<T>(
                        resource: upHandle.Resource, 
                        disposeAction: () =>
                        {
                            upHandle.Dispose();

                            lock (l)
                            {
                                waiting--;
                                if (waiting == 0)
                                {
                                    mutex.Release();
                                    mutex.Dispose();
                                    mutex = null;
                                }
                                mutex?.Release();
                            }
                        }
                    );
                });
        }

        /// <summary>
        /// Share a resource that may be accessed in parallel. 
        /// Make sure that the resource is not mutating while access is granted.
        /// You may specify how long the resource stays valid after the RefCount goes to zero.
        /// </summary> 
        public static IResourceProvider<T> ShareInParallel<T>(this IResourceProvider<T> source, int delayDisposalInMilliseconds = 0)
            => source.Publish().RefCount(delayDisposalInMilliseconds); // resetting not necessary. Resource should not mutate.

        /// <summary>
        /// Share a resource that may be accessed in parallel. 
        /// Make sure that the resource is not mutating while access is granted.
        /// You may specify an trigger source which signals the disposal after the RefCount goes to zero.
        /// </summary> 
        public static IResourceProvider<T> ShareInParallel<T,U>(this IResourceProvider<T> source, IObservable<U> disposalTriggerSource)
            => source.Publish().RefCount(disposalTriggerSource); // resetting not necessary. Resource should not mutate.

        /// <summary>
        /// Share a resource that may be accessed in a serial fashion only. 
        /// </summary>
        public static IResourceProvider<T> ShareSerially<T>(this IResourceProvider<T> source)
            => source.Serialize(); // reset action not required. resource doesn't get shared. Only access to resource is shared - serially.

        /// <summary>
        /// Share a resource that may be accessed in a serial fashion only. 
        /// You may specify how long the resource stays valid. 
        /// Make sure you reset the resource in a way that it feels like a fresh resource.
        /// It only gets called when a resource gets actually reused.
        /// </summary>
        public static IResourceProvider<T> ShareSerially<T>(this IResourceProvider<T> source, int delayDisposalInMilliseconds, Action<T> resetResource)
            => source.Publish(resetResource).RefCount(delayDisposalInMilliseconds).Serialize();

        /// <summary>
        /// Share a resource that may be accessed in a serial fashion only. 
        /// You may specify an trigger source which signals the disposal after the RefCount goes to zero.
        /// Make sure you reset the resource in a way that it feels like a fresh resource.
        /// It only gets called when a resource gets actually reused.
        /// </summary>
        public static IResourceProvider<T> ShareSerially<T,U>(this IResourceProvider<T> source, IObservable<U> disposalTriggerSource, Action<T> resetResource)
            => source.Publish(resetResource).RefCount(disposalTriggerSource).Serialize();

        /// <summary>
        /// Share resources that may be accessed in a serial fashion only.
        /// Manages a pool of resources, will provide either a resource from the pool or a new one if the pool is empty.
        /// You may specify how long the resources in the pool stay valid after the RefCount goes to zero.
        /// Make sure you reset the resource in a way that it feels like a fresh resource.
        /// Note that even a delayDisposalInMilliseconds of 0 might lead to a reuse of a resource if several threads are accessing the pool.
        /// This is why you should always provide a valid reset method. It only gets called when a resource gets actually reused.
        /// </summary>
        public static IResourceProvider<T> SharePooled<T>(this IResourceProvider<T> source, int delayDisposalInMilliseconds, Action<T> resetResource)
            => source.PublishPooled(resetResource).RefCount(delayDisposalInMilliseconds);

        /// <summary>
        /// Share resources that may be accessed in a serial fashion only.
        /// Manages a pool of resources, will provide either a resource from the pool or a new one if the pool is empty.
        /// You may specify an trigger source which signals the disposal after the RefCount goes to zero.
        /// Make sure you reset the resource in a way that it feels like a fresh resource.
        /// Note that even a delayDisposalInMilliseconds of 0 might lead to a reuse of a resource if several threads are accessing the pool.
        /// This is why you should always provide a valid reset method. It only gets called when a resource gets actually reused.
        /// </summary>
        public static IResourceProvider<T> SharePooled<T,U>(this IResourceProvider<T> source, IObservable<U> disposalTriggerSource, Action<T> resetResource)
            => source.PublishPooled(resetResource).RefCount(disposalTriggerSource);

        /// <summary>
        /// Cata
        /// Empty using statement
        /// Only use for sideeffects of the upstream ResourceProvider Monad
        /// </summary>
        public static IResourceProvider<T> Using<T>(this IResourceProvider<T> provider)
        {
            using (var handle = provider.GetHandle()) { };
            return provider;
        }

        /// <summary>
        /// Cata
        /// Runs the action on the resource
        /// </summary>
        public static IResourceProvider<T> Using<T>(this IResourceProvider<T> provider, Action<T> action)
        {
            using (var handle = provider.GetHandle())
                action(handle.Resource);

            return provider;
        }

        /// <summary>
        /// Cata
        /// Runs the extractor on the resource and returns the output.
        /// </summary>
        public static TOut Using<T, TOut>(this IResourceProvider<T> provider, Func<T, TOut> extractor)
        {
            using (var handle = provider.GetHandle())
                return extractor(handle.Resource);
        }

        #region Zip
        /// <summary>
        /// Return a resource using two source resources.
        /// Does not take ownership of resource in the resulting provider.
        /// </summary>
        public static IResourceProvider<TOut> Bind<T1, T2, TOut>(this IResourceProvider<T1> first, IResourceProvider<T2> second, Func<T1, T2, TOut> resultSelector)
        {
            return first.Bind(a => second.Bind(b => resultSelector(a, b)));
        }

        /// <summary>
        /// Return a resource using three source resources.
        /// Does not take ownership of resource in the resulting provider.
        /// </summary>
        public static IResourceProvider<TOut> Bind<T1, T2, T3, TOut>(this IResourceProvider<T1> first, IResourceProvider<T2> second, IResourceProvider<T3> third, Func<T1, T2, T3, TOut> resultSelector)
        {
            return first.Bind(a => second.Bind(b => third.Bind(c => resultSelector(a, b, c))));
        }

        /// <summary>
        /// Return a resource using four source resources.
        /// Does not take ownership of resource in the resulting provider.
        /// </summary>
        public static IResourceProvider<TOut> Bind<T1, T2, T3, T4, TOut>(this IResourceProvider<T1> first, IResourceProvider<T2> second, IResourceProvider<T3> third, IResourceProvider<T4> fourth, Func<T1, T2, T3, T4, TOut> resultSelector)
        {
            return first.Bind(a => second.Bind(b => third.Bind(c => fourth.Bind(d => resultSelector(a, b, c, d)))));
        }

        #endregion Zip

        #region ZipNew
        public static IResourceProvider<TOut> BindNew<T1, T2, TOut>(this IResourceProvider<T1> first, IResourceProvider<T2> second, Func<T1, T2, TOut> resultSelector)
        {
            return first.Bind(a => second.Bind(b => New(() => resultSelector(a, b))));
        }

        /// <summary>
        /// Create a new resource using three source resources.
        /// Takes ownership of the new resource in the resulting provider.
        /// </summary>
        public static IResourceProvider<TOut> BindNew<T1, T2, T3, TOut>(this IResourceProvider<T1> first, IResourceProvider<T2> second, IResourceProvider<T3> third, Func<T1, T2, T3, TOut> resultSelector)
        {
            return first.Bind(a => second.Bind(b => third.Bind(c => New(() => resultSelector(a, b, c)))));
        }

        /// <summary>
        /// Create a new resource using four source resources.
        /// Takes ownership of the new resource in the resulting provider.
        /// </summary>
        public static IResourceProvider<TOut> BindNew<T1, T2, T3, T4, TOut>(this IResourceProvider<T1> first, IResourceProvider<T2> second, IResourceProvider<T3> third, IResourceProvider<T4> fourth, Func<T1, T2, T3, T4, TOut> resultSelector)
        {
            return first.Bind(a => second.Bind(b => third.Bind(c => fourth.Bind(d => New(() => resultSelector(a, b, c, d))))));
        }

        #endregion ZipNew

        public static IResourceProvider<T> SynchronouslyWorkOnSingleResource<T>(this IResourceProvider<T> source, Action<IResourceProvider<T>> action)
        {
            using (var h = source.GetHandle())
            {
                action(Return(h.Resource));
            }
            return source;
        }

        /// <summary>
        /// Act on the resource right before it gets disposed.
        /// </summary>
        public static IResourceProvider<T> Finally<T>(this IResourceProvider<T> source, Action<T> onDispose)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new Provider<T>(
                producer: () =>
                {
                    var handle = source.GetHandle();
                    return new Handle<T>(
                        resource: handle.Resource, 
                        disposeAction: () =>
                        {
                            try
                            {
                                onDispose(handle.Resource);
                            }
                            finally
                            {
                                handle.Dispose();
                            }
                        });
                });
        }
    }

    /// <summary>
    /// Generic implementation that can be used for any on the fly implementation. 
    /// Helps with correct implementation of IDispoable.
    /// </summary>
    sealed class Handle<TResource> : IResourceHandle<TResource>
    {
        readonly IDisposable FDisposable;

        public Handle(TResource resource, Action disposeAction)
        {
            Resource = resource;
            FDisposable = Disposable.Create(disposeAction); // makes sure that dispose only gets called once
        }

        public TResource Resource { get; }
        public void Dispose() => FDisposable.Dispose();
    }

    /// <summary>
    /// Generic implementation that can be used for any on the fly implementation. 
    /// Helps with correct implementation of IDispoable.
    /// </summary>
    sealed class Handle<TResource, TState> : IResourceHandle<TResource>
    {
        readonly IDisposable FDisposable;

        public Handle(TResource resource, TState state, Action<TState> disposeAction)
        {
            Resource = resource;
            FDisposable = Disposable.Create(state, disposeAction); // makes sure that dispose only gets called once
        }

        public TResource Resource { get; }
        public void Dispose() => FDisposable.Dispose();
    }

    /// <summary>
    /// Generic implementation that can be used for any on the fly implementation.
    /// </summary>
    class Provider<TResource> : IResourceProvider<TResource>
    {
        readonly Func<IResourceHandle<TResource>> FProducer;

        public Provider(Func<IResourceHandle<TResource>> producer)
        {
            FProducer = producer;
        }
        public IResourceHandle<TResource> GetHandle() => FProducer();
    }

    /// <summary>
    /// Generic implementation that can be used for any on the fly implementation. 
    /// Helps with correct implementation of IDispoable returned by Connect().
    /// </summary>
    sealed class ConnectableProvider<TResource> : Provider<TResource>, IConnectableResourceProvider<TResource>
    {
        Func<Action> FConnectAction;

        public ConnectableProvider(Func<IResourceHandle<TResource>> producer, Func<Action> connectAction)
            : base(producer)
        {
            FConnectAction = connectAction;
        }

        public IDisposable Connect() => Disposable.Create(FConnectAction());
    }

    public sealed class ResourceProviderMonitor<T> : IResourceProvider<T>
    {
        IResourceProvider<T> Source;
        object Lock = new object();

        public ResourceProviderMonitor(IResourceProvider<T> source)
        {
            Source = source;
            CurrentResources = new List<T>();
        }

        public int SinkCount { get; private set; }
        public IReadOnlyList<T> ResourcesUsedBySinks => CurrentResources;
        private List<T> CurrentResources;

        public IResourceHandle<T> GetHandle()
        {
            lock (Lock)
            {
                var upHandle = Source.GetHandle();
                SinkCount++;
                var r = upHandle.Resource;
                CurrentResources.Add(r);
                return new Handle<T>(
                    resource: r,
                    disposeAction: () =>
                    {
                        lock (Lock)
                        {
                            try
                            {
                                upHandle.Dispose();
                            }
                            finally
                            {
                                SinkCount--;
                                CurrentResources.Remove(r);
                            }
                        }
                    });
            }
        }
    }

    /// <summary>
    /// Takes a resourceprovider and outputs its resource
    /// Makes sure to call GetHandle before releasing the old handle.
    /// </summary>
    public sealed class GetLatestResourceForTemporaryUse<TResource> : IDisposable
    {
        private readonly SerialDisposable FDisposable = new SerialDisposable();

        public TResource GetNewResource(IResourceProvider<TResource> provider)
        {
            IResourceHandle<TResource> handle = provider.GetHandle();
            FDisposable.Disposable = handle;
            return handle.Resource;
        }

        public void Dispose() => FDisposable.Dispose();
    }






    internal static class DictHelper
    {
        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> producer)
        {
            if (!dict.TryGetValue(key, out TValue value))
                dict[key] = value = producer();

            return value;
        }
    }
}