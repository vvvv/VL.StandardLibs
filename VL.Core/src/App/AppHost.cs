#nullable enable
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reflection;
using System.Threading;
using VL.Core.CompilerServices;

namespace VL.Core
{
    /// <summary>
    /// Implemented by all the entry points (vvvv.exe, exported apps, runtime instances).
    /// All entry points will make themselfs current before calling into the patch (<see cref="MakeCurrent"/>).
    /// Patches will capture the current app host and restore it in callbacks should no other app host be current (<see cref="MakeCurrentIfNone"/>).
    /// </summary>
    public abstract class AppHost
    {
        /// <summary>
        /// The app host for the current thread. Throws <see cref="InvalidOperationException"/> in case no host is installed.
        /// </summary>
        public static AppHost Current => current ?? throw new InvalidOperationException("No app host is installed on the current thread.");

        /// <summary>
        /// The app host for the current thread or the global one if there's no host installed on the current thread.
        /// </summary>
        public static AppHost CurrentOrGlobal => current ?? Global;

        /// <summary>
        /// The app host for the whole application. 
        /// When running inside the editor this refers to the development environment. 
        /// When running as a standalone app it's the same as <see cref="Current"/>.
        /// </summary>
        public static AppHost Global => global ?? throw new InvalidOperationException("No global app host is installed.");

        /// <summary>
        /// Whether or not a context is installed on the current thread.
        /// </summary>
        internal static bool IsCurrent() => current != null;

        private static AppHost? global;

        [ThreadStatic]
        private static AppHost? current;

        public AppHost()
        {
            MakeGlobalIfNone().DisposeBy(this);

            IDisposable MakeGlobalIfNone()
            {
                var original = Interlocked.CompareExchange(ref global, this, null);
                return Disposable.Create(() => Interlocked.CompareExchange(ref global, original, this));
            }
        }

        /// <summary>
        /// Make the app host current on the current thread.
        /// </summary>
        /// <returns>A disposable which will restore the previous app host on dispose.</returns>
        public Frame MakeCurrent()
        {
            return new Frame(this);
        }

        /// <summary>
        /// Make the app host current on the current thread if no app host is current yet.
        /// </summary>
        /// <returns>A disposable which will restore the previous app host on dispose.</returns>
        public Frame MakeCurrentIfNone()
        {
            return new Frame(current ?? this);
        }

        public readonly struct Frame : IDisposable
        {
            readonly AppHost? saved;

            internal Frame(AppHost context)
            {
                saved = current;
                current = context;
            }

            public void Dispose()
            {
                current = saved;
            }
        }

        /// <summary>
        /// The base path of the currently running application.
        /// </summary>
        public abstract string AppBasePath { get; }

        /// <summary>
        /// Whether the app is exported and runs standalone as an executable.
        /// </summary>
        public abstract bool IsExported { get; }

        /// <summary>
        /// The service registry of the app.
        /// </summary>
        public abstract ServiceRegistry Services { get; }

        /// <summary>
        /// The application patch.
        /// </summary>
        public abstract IVLObject App { get; }

        /// <summary>
        /// Raised when the application exits.
        /// </summary>
        public abstract IObservable<Unit> OnExit { get; }

        /// <summary>
        /// Ties the lifetime of the component to the one of the host.
        /// </summary>
        public abstract void TakeOwnership(IDisposable component);

        /// <summary>
        /// Tells the app host to stay alive until the returned disposable is disposed.
        /// </summary>
        public abstract IDisposable KeepAlive();

        /// <summary>
        /// The document base path. Running inside the editor this refers to the directory of the document otherwise the directory of the executable.
        /// </summary>
        public abstract string GetDocumentBasePath(UniqueId documentId);

        /// <summary>
        /// The path of the locally installed package. If the package is not installed or the app is running as standalone this will return null.
        /// </summary>
        public abstract string? GetPackagePath(string packageId);

        /// <summary>
        /// Create a new instance of the given type by calling it's constructor using default values for any of its arguments.
        /// If there's no default constructor registered it will fallback to the default value.
        /// </summary>
        /// <param name="type">The type to create.</param>
        /// <param name="nodeContext">The node context to use. Used by patched types.</param>
        /// <returns>The new instance.</returns>
        public abstract object CreateInstance(Type type, NodeContext? nodeContext = default);

        /// <summary>
        /// Retrieves the default value of the given type. 
        /// If there's no default value registered it will return the CLR default.
        /// </summary>
        /// <returns>The default of the given type.</returns>
        public abstract object GetDefaultValue(Type type);

        public T? CreateInstance<T>(NodeContext? nodeContext = default) => CreateInstance(typeof(T), nodeContext) is T t ? t : default;

        public T? GetDefaultValue<T>() => GetDefaultValue(typeof(T)) is T t ? t : default;

        internal abstract void RegisterServices(Assembly assembly);

        internal abstract void RegisterServices(AssemblyInitializer assemblyInitializer);
    }

    //public class NestedApp : IDisposable
    //{
    //    private readonly AppHost appHost = AppHost.Current;

    //    private object? state;
    //    private NestedAppHost? innerHost;

    //    public void Update(Func<object> create, Action<object> update)
    //    {
    //        if (innerHost is null)
    //            innerHost = new NestedAppHost(appHost);

    //        using (innerHost.MakeCurrent())
    //        {
    //            if (state is null)
    //                state = create();

    //            update(state);
    //        }
    //    }

    //    public void Reset()
    //    {
    //        if (innerHost is null)
    //            return;

    //        try
    //        {
    //            using (innerHost.MakeCurrent())
    //            {
    //                if (state is IDisposable disposable)
    //                    disposable.Dispose();

    //                innerHost.Dispose();
    //            }
    //        }
    //        finally
    //        {
    //            state = null;
    //            innerHost = null;
    //        }
    //    }

    //    public void Dispose()
    //    {
    //        Reset();
    //    }

    //    private class NestedAppHost : AppHost, IDisposable
    //    {
    //        private readonly AppHost parent;
    //        private readonly IDisposable parentSubscription;

    //        public NestedAppHost(AppHost parent)
    //        {
    //            this.parent = parent;
    //            this.parentSubscription = parent.KeepAlive();
    //        }

    //        public override string AppBasePath => parent.AppBasePath;

    //        public override bool IsExported => parent.IsExported;

    //        public override ServiceRegistry Services => parent.Services;

    //        public override ICollection<IDisposable> Components => parent.Components;

    //        public override IObservable<Unit> OnExit => parent.OnExit;

    //        public void Dispose()
    //        {
    //            parentSubscription.Dispose();
    //        }

    //        public override string GetDocumentBasePath(UniqueId documentId)
    //        {
    //            return parent.GetDocumentBasePath(documentId);
    //        }

    //        public override string? GetPackagePath(string packageId)
    //        {
    //            return parent.GetPackagePath(packageId);
    //        }

    //        public override IDisposable KeepAlive()
    //        {
    //            return parent.KeepAlive();
    //        }
    //    }
    //}
}
#nullable restore