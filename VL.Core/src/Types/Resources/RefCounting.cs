using Stride.Core;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using VL.Core;
using ServiceRegistry = VL.Core.ServiceRegistry;

namespace VL.Lib.Basics.Resources
{
    /// <summary>
    /// Abstraction used by <see cref="Using{T}"/> and <see cref="Producing{T}"/> to enable reference counting on specific resources.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    public interface IRefCounter<in T>
    {
        /// <summary>
        /// Sets the reference count on the resource to one.
        /// Throws <see cref="InvalidOperationException"/> in case reference counting is already unlocked on the resource.
        /// </summary>
        /// <remarks>
        /// Must only be called by producers.
        /// </remarks>
        /// <param name="resource">The resource whose reference count shall be one</param>
        void Init(T resource);

        /// <summary>
        /// Increases the referece count on the resource (if reference counting is available).
        /// </summary>
        /// <param name="resource">The resource whose reference count shall be increased</param>
        void AddRef(T resource);

        /// <summary>
        /// Decreases the reference count on the resource (if reference counting is available).
        /// </summary>
        /// <param name="resource">The resource whose reference count shall be decreaed</param>
        void Release(T resource);
    }

    /// <summary>
    /// Keeps the given resource alive by increasing its reference count.
    /// It will only do so if reference counting was indeed made available by a <see cref="Producing{T}"/> node.
    /// </summary>
    /// <remarks>
    /// Internally two references are kept called front and back. The setter uses the back reference, while the getter swaps front and back in case
    /// a new resource has been set. This ensures that the latest retrieved resource will be alive even when a new one gets set on another thread.
    /// The two references also ensure that a resource doesn't get released too early during a frame.
    /// </remarks>
    /// <typeparam name="T">The type of the resource.</typeparam>
    public sealed class Using<T> : IDisposable
    {
        private static readonly IRefCounter<T> refCounter = RefCounting.GetRefCounter<T>();

        private readonly object readWriteLock = new object();
        private bool needsSwap;
        private T front, back;

        // For inlining to work the method body needs to be small
        public T Resource
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => refCounter is null ? front : SynchronizedGet();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (refCounter is null)
                    front = value;
                else
                    SynchronizedSet(value);
            }
        }

        T SynchronizedGet()
        {
            lock (readWriteLock)
            {
                if (needsSwap)
                {
                    needsSwap = false;
                    Utilities.Swap(ref front, ref back);
                }
            }

            return front;
        }

        void SynchronizedSet(T value)
        {
            lock (readWriteLock)
            {
                // Incrementing first is the safer approach
                refCounter.AddRef(value);
                refCounter.Release(back);
                back = value;
                needsSwap = true;
            }
        }

        public void Dispose()
        {
            refCounter?.Release(front);
            refCounter?.Release(back);

            // Give up references as well - will also protect from double disposal
            front = default;
            back = default;
        }
    }

    /// <summary>
    /// Manages the lifetime of a resource through reference counting (if available for the specific resource type).
    /// </summary>
    /// <remarks>
    /// A <seealso cref="IRefCounter{T}"/> must be registered for the resource type to unlock its reference counting.
    /// </remarks>
    /// <typeparam name="T">The type of the resource.</typeparam>
    public sealed class Producing<T> : IDisposable
    {
        private static readonly IRefCounter<T> refCounter = RefCounting.GetRefCounter<T>();
        private T resource;

        public T Resource
        {
            get => resource;
            set
            {
                // Unlock ref counting
                refCounter?.Init(value);
                refCounter?.Release(resource);
                resource = value;
            }
        }

        public void Dispose()
        {
            refCounter?.Release(resource);
            resource = default;
        }
    }

    public static class RefCounting
    {
        public static IRefCounter<T> GetRefCounter<T>()
        {
            // To be compatible with older code (like VL.Video.MediaFoundation) we need query the IVLFactory first
            // It will fallback to the ServiceRegistry
            var factory = AppHost.CurrentOrGlobal.Factory;
            return factory.GetService<IRefCounter<T>>();
        }

        abstract class RefCount
        {
            public static readonly RefCount Static = new StaticRefCount();
            public static RefCount New() => new DynamicRefCount();

            public Action AfterDispose;

            public abstract int AddRef();
            public abstract int Release();

            sealed class DynamicRefCount : RefCount
            {
                private int Value;

                public override int AddRef() => Interlocked.Increment(ref Value);
                public override int Release() => Interlocked.Decrement(ref Value);
            }

            sealed class StaticRefCount : RefCount
            {
                public override int AddRef() => 1;
                public override int Release() => 1;
            }
        }

        private static readonly ConditionalWeakTable<IDisposable, RefCount> s_refCounts = new ConditionalWeakTable<IDisposable, RefCount>();

        public static T RefCounted<T>(this T resource)
            where T : class, IDisposable
        {
            if (resource is null)
                throw new ArgumentNullException(nameof(resource));

            var refCount = s_refCounts.GetValue(resource, _ => RefCount.New());
            refCount.AddRef();
            return resource;
        }

        public static T NotRefCounted<T>(this T resource)
            where T : class, IDisposable
        {
            if (resource is null)
                throw new ArgumentNullException(nameof(resource));

            if (s_refCounts.TryGetValue(resource, out _))
                throw new InvalidOperationException("Ref counting has already been enabled on this resources.");

            s_refCounts.Add(resource, RefCount.Static);

            return resource;
        }

        public static T AddRef<T>(this T resource)
            where T : class, IDisposable
        {
            if (resource is null)
                throw new ArgumentNullException(nameof(resource));

            if (s_refCounts.TryGetValue(resource, out var refCount))
            {
                var count = refCount.AddRef();

                //Trace.TraceInformation($"RefCount on {resource.GetHashCode()} is {count}");
            }

            return resource;
        }

        public static void Release<T>(this T resource)
            where T : class, IDisposable
        {
            if (resource is null)
                throw new ArgumentNullException(nameof(resource));

            if (s_refCounts.TryGetValue(resource, out var refCount))
            {
                var count = refCount.Release();
                Debug.Assert(count >= 0);
                if (count == 0)
                {
                    resource.Dispose();
                    refCount.AfterDispose?.Invoke();
                }

                //Trace.TraceInformation($"RefCount on {resource.GetHashCode()} is {count}");
            }
        }

        public static T AfterDispose<T>(this T resource, Action action)
            where T : IDisposable
        {
            if (resource is null)
                throw new ArgumentNullException(nameof(resource));

            if (!s_refCounts.TryGetValue(resource, out var refCount))
                throw new InvalidOperationException($"No ref count installed for {resource}");

            refCount.AfterDispose = action;

            return resource;
        }
    }
}
