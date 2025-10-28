﻿#nullable enable
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using LoggerFactory = VL.Core.Logging.LoggerFactory;

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
        internal static AppHost? CurrentOrGlobalOrNull => current ?? global;

        /// <summary>
        /// The app host for the whole application. 
        /// When running inside the editor this refers to the development environment. 
        /// When running as a standalone app it's the same as <see cref="Current"/>.
        /// </summary>
        public static AppHost Global => global ?? throw new InvalidOperationException("No global app host is installed.");

        /// <summary>
        /// Whether or not a context is installed on the current thread.
        /// </summary>
        internal static bool IsAnyCurrent() => current != null;

        internal static ILogger? CurrentDefaultLogger => current?.DefaultLogger;

        private static AppHost? global;

        [ThreadStatic]
        private static AppHost? current;

        private IDisposable? globalSubscription;

        public AppHost()
        {
            globalSubscription = MakeGlobalIfNone();

            IDisposable MakeGlobalIfNone()
            {
                var original = Interlocked.CompareExchange(ref global, this, null);
                return Disposable.Create(() => Interlocked.CompareExchange(ref global, original, this));
            }
        }

        protected internal void Shutdown()
        {
            var s = Interlocked.Exchange(ref globalSubscription, null);
            s?.Dispose();
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

        /// <summary>
        /// Whether or not this app host is current on the current thread.
        /// </summary>
        public bool IsCurrent => current == this;

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
        /// The file path of the currently running application.
        /// </summary>
        public abstract string AppPath { get; }

        /// <summary>
        /// The name of the currently running application.
        /// </summary>
        /// <remarks>
        /// Inside the editor this refers to the VL document name while in an exported app this refers to the exe name.
        /// </remarks>
        public string AppName => Path.GetFileNameWithoutExtension(AppPath);

        /// <summary>
        /// The directory of the currently running application.
        /// </summary>
        public virtual string AppBasePath => Path.GetDirectoryName(AppPath)!;

        /// <summary>
        /// Whether the app is exported and runs standalone as an executable.
        /// </summary>
        public virtual bool IsExported => false;

        /// <summary>
        /// Whether the app is running in the user thread.
        /// </summary>
        public virtual bool IsUser => false;

        /// <summary>
        /// The service registry of the app.
        /// </summary>
        public abstract ServiceRegistry Services { get; }

        /// <summary>
        /// The type registry of the app.
        /// </summary>
        public abstract TypeRegistry TypeRegistry { get; }

        /// <summary>
        /// Contains all the node factories registered for this app.
        /// </summary>
        public abstract NodeFactoryRegistry NodeFactoryRegistry { get; }

        /// <summary>
        /// The node factory cache used by the app. Usually there's only one per process.
        /// </summary>
        public abstract NodeFactoryCache NodeFactoryCache { get; }

        /// <summary>
        /// The synchronization context of the app. Allows to interact with its main thread.
        /// </summary>
        public abstract SynchronizationContext SynchronizationContext { get; }

        /// <summary>
        /// Runs posted tasks inside of the update loop of the app.
        /// </summary>
        public abstract TaskScheduler MainLoopTaskScheduler { get; }

        /// <summary>
        /// The VL factory of the app. This property exists only for compatibility reasons.
        /// </summary>
        [Browsable(false)]
        public abstract IVLFactory Factory { get; }

        /// <summary>
        /// The logger factory of the app. Logging gets configured by <see cref="IStartup.SetupLogging(AppHost, Microsoft.Extensions.Logging.ILoggingBuilder)"/>.
        /// </summary>
        public abstract LoggerFactory LoggerFactory { get; }

        public abstract ILogger DefaultLogger { get; }

        /// <summary>
        /// The configuration of the app. The configuration gets initialized by <see cref="IStartup.SetupConfiguration(AppHost, IConfigurationBuilder)"/>.
        /// </summary>
        public abstract IConfiguration Configuration { get; }

        /// <summary>
        /// The application patch.
        /// </summary>
        public abstract IVLObject App { get; }

        /// <summary>
        /// Raised when the application exits.
        /// </summary>
        public abstract IObservable<Unit> OnExit { get; }

        /// <summary>
        /// Whether or not the host is disposed.
        /// </summary>
        public abstract bool IsDisposed { get; }

        /// <summary>
        /// Ties the lifetime of the component to the one of the host.
        /// </summary>
        public abstract void TakeOwnership(IDisposable component);

        /// <summary>
        /// Tells the app host to stay alive until the returned disposable is disposed.
        /// </summary>
        public abstract IDisposable KeepAlive();

        /// <summary>
        /// The document path. Running inside the editor this refers to the path of the document otherwise the path of the executable.
        /// </summary>
        public abstract string GetDocumentPath(UniqueId documentId);

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
        /// <param name="arguments">The arguments to use.</param>
        /// <returns>The new instance.</returns>
        public abstract object? CreateInstance(IVLTypeInfo type, NodeContext? nodeContext = default, IReadOnlyDictionary<string, object?>? arguments = null);

        /// <summary>
        /// Retrieves the default value of the given type. 
        /// If there's no default value registered it will return the CLR default.
        /// </summary>
        /// <returns>The default of the given type.</returns>
        public abstract object? GetDefaultValue(IVLTypeInfo type);

        public T? CreateInstance<T>(NodeContext? nodeContext = default) => CreateInstance(typeof(T), nodeContext) is T t ? t : default;

        public T? GetDefaultValue<T>() => GetDefaultValue(typeof(T)) is T t ? t : default;

        public void ReportException(ExceptionDispatchInfo exceptionDispatchInfo)
        {
            var exception = exceptionDispatchInfo.SourceException;
            DefaultLogger.LogCritical(exception, exception.Message);
            OnException(exceptionDispatchInfo);
        }

        protected abstract void OnException(ExceptionDispatchInfo exceptionDispatchInfo);

        internal object? CreateInstance(Type type, NodeContext? nodeContext = default, IReadOnlyDictionary<string, object?>? arguments = null) 
            => CreateInstance(TypeRegistry.GetTypeInfo(type), nodeContext, arguments);

        internal object? GetDefaultValue(Type type) => GetDefaultValue(TypeRegistry.GetTypeInfo(type));

        internal abstract SerializationService SerializationService { get; }

        internal abstract NodeContext RootContext { get; }

        /// <summary>
        /// Loads a plugin from the specified directory. The specified directory should contain the plugin dll of the same name. For example myPlugin/myPlugin.dll
        /// </summary>
        /// <param name="path">The directory of the plugin to load.</param>
        /// <returns>A <see cref="PluginInfo"/> object containing information about the loaded plugin.</returns>
        public abstract PluginInfo LoadPlugin(string path);

        /// <summary>
        /// Occurs when a plugin is successfully loaded.
        /// </summary>
        /// <remarks>This event is triggered after a plugin has been loaded into the application. 
        /// Subscribers can use this event to perform actions such as initializing plugin-specific settings or updating
        /// the user interface to reflect the newly available plugin functionality.</remarks>
        public event EventHandler<PluginLoadedEventArgs> PluginLoaded;

        /// <summary>
        /// Called when a plugin got loaded. Raises the <see cref="PluginLoaded"/> event.
        /// </summary>
        protected virtual void OnPluginLoaded(PluginInfo pluginInfo)
        {
            PluginLoaded?.Invoke(this, new PluginLoadedEventArgs(this, pluginInfo));
        }
    }

    public sealed class PluginLoadedEventArgs : EventArgs
    {
        public AppHost AppHost { get; }
        public PluginInfo PluginInfo { get; }
        public PluginLoadedEventArgs(AppHost appHost, PluginInfo pluginInfo)
        {
            AppHost = appHost;
            PluginInfo = pluginInfo;
        }
    }

    public record PluginInfo(Assembly Assembly, string Path, string Name);

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