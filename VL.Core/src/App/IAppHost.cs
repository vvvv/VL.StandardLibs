#nullable enable
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reflection;
using System.Text;
using System.Threading;

namespace VL.Core
{
    /// <summary>
    /// Implemented by all the entry points (vvvv.exe, exported apps, user runtime instances).
    /// All entry points will make themselfs current before calling into the patch (<see cref="AppHostExtensions.MakeCurrent"/>).
    /// Patches will capture the current app host and restore it in callbacks should no other app host be current (<see cref="AppHostExtensions.MakeCurrentIfNone"/>).
    /// </summary>
    public interface IAppHost
    {
        /// <summary>
        /// The app host for the current thread. Throws <see cref="InvalidOperationException"/> in case no registry is installed.
        /// </summary>
        public static IAppHost Current => AppHostExtensions.Current;

        /// <summary>
        /// The app host for the current thread or the global one if there's no registry installed on the current thread.
        /// </summary>
        public static IAppHost CurrentOrGlobal => AppHostExtensions.CurrentOrGlobal;

        /// <summary>
        /// The app host for the whole application.
        /// </summary>
        public static IAppHost Global => AppHostExtensions.Global;

        /// <summary>
        /// Whether or not a context is installed on the current thread.
        /// </summary>
        public static bool IsCurrent() => AppHostExtensions.IsCurrent();

        /// <summary>
        /// The base path of the currently running application.
        /// </summary>
        string AppBasePath { get; }

        /// <summary>
        /// Whether the app is exported and runs standalone as an executable.
        /// </summary>
        bool IsExported { get; }

        /// <summary>
        /// The service registry of the app.
        /// </summary>
        ServiceRegistry Services { get; }

        /// <summary>
        /// Can be used to tie the lifetime of an object to the one of the application.
        /// </summary>
        ICollection<IDisposable> Components { get; }

        /// <summary>
        /// Raised when the application exits.
        /// </summary>
        IObservable<Unit> OnExit { get; }

        /// <summary>
        /// Tells the app host to stay alive until the returned disposable is disposed.
        /// </summary>
        IDisposable KeepAlive();

        /// <summary>
        /// The document base path. Running inside the editor this refers to the directory of the document otherwise the directory of the executable.
        /// </summary>
        string GetDocumentBasePath(UniqueId documentId);

        /// <summary>
        /// The path of the locally installed package. If the package is not installed or the app is running as standalone this will return null.
        /// </summary>
        string? GetPackagePath(string packageId);
    }

    public static class AppHostExtensions
    {
        private static IAppHost? global;

        [ThreadStatic]
        private static IAppHost? current;

        internal static IAppHost Current => current ?? throw new InvalidOperationException("No app host is installed on the current thread.");
        internal static IAppHost Global => global ?? throw new InvalidOperationException("No global app host is installed.");
        internal static IAppHost CurrentOrGlobal => current ?? Global;

        internal static bool IsCurrent() => current != null;

        /// <summary>
        /// Make the app host current on the current thread.
        /// </summary>
        /// <returns>A disposable which will restore the previous app host on dispose.</returns>
        public static Frame MakeCurrent(this IAppHost appHost)
        {
            return new Frame(appHost);
        }

        /// <summary>
        /// Make the app host current on the current thread if no app host is current yet.
        /// </summary>
        /// <returns>A disposable which will restore the previous app host on dispose.</returns>
        public static Frame MakeCurrentIfNone(this IAppHost appHost)
        {
            return new Frame(current ?? appHost);
        }

        public static IDisposable MakeGlobalIfNone(this IAppHost appHost)
        {
            var original = Interlocked.CompareExchange(ref global, appHost, null);
            return Disposable.Create(() => Interlocked.CompareExchange(ref global, original, appHost));
        }

        public readonly struct Frame : IDisposable
        {
            readonly IAppHost? saved;

            internal Frame(IAppHost context)
            {
                saved = current;
                current = context;
            }

            public void Dispose()
            {
                current = saved;
            }
        }
    }
}
#nullable restore