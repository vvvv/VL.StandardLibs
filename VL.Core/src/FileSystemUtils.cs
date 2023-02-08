using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reactive.Linq;

namespace VL.Core
{
    public static class FileSystemUtils
    {
        public static IObservable<FileSystemEventArgs> WatchDir(string path, string filter = "*.*", bool includeSubdirectories = false)
        {
            if (path is null)
                throw new ArgumentNullException(nameof(path));
            if (path == string.Empty)
                throw new ArgumentException("Path is empty", nameof(path));

            return watchers.GetOrAdd((path, filter, includeSubdirectories), d =>
            {
                return Observable.Using(
                    () => new FileSystemWatcher(d.path, d.filter)
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                        IncludeSubdirectories = d.includeSubdirectories,
                        EnableRaisingEvents = true
                    },
                    w =>
                    {
                        return Observable.Merge(
                            Observable.FromEventPattern<FileSystemEventArgs>(w, nameof(FileSystemWatcher.Changed)).Select(e => e.EventArgs),
                            Observable.FromEventPattern<FileSystemEventArgs>(w, nameof(FileSystemWatcher.Deleted)).Select(e => e.EventArgs),
                            Observable.FromEventPattern<FileSystemEventArgs>(w, nameof(FileSystemWatcher.Created)).Select(e => e.EventArgs),
                            Observable.FromEventPattern<RenamedEventArgs>(w, nameof(FileSystemWatcher.Renamed)).Select(e => e.EventArgs));
                    }).Publish().RefCount();
            });
        }
        private static readonly ConcurrentDictionary<(string path, string filter, bool includeSubdirectories), IObservable<FileSystemEventArgs>> watchers = new ConcurrentDictionary<(string path, string filter, bool includeSubdirectories), IObservable<FileSystemEventArgs>>();
    }
}
