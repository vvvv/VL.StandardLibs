using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using VL.Core;
using VL.Core.Import;
using VL.Lib.IO;

[assembly: ImportType(typeof(Watcher), Name = "FileWatcher", Category = "IO.Experimental")]

namespace VL.Lib.IO
{
    public static class RenamedEventArgsUtils
    {
        /// <summary>
        /// Returns the new path of a rename reported by watcher
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static Path NewPath(this RenamedEventArgs e) => new Path(e.FullPath);

        /// <summary>
        /// Returns the old path of a rename reported by watcher
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static Path OldPath(this RenamedEventArgs e) => new Path(e.OldFullPath);
    }

    /// <summary>
    /// Monitors a folder and its files for creation, change, deletion and renaming
    /// </summary>
    [ProcessNode]
    public class Watcher : IDisposable
    {
        private FileSystemWatcher FWatcher;
        private string FPath;
        private string FFilter;
        private bool FIncludeSubDirs;

        public void Update(string path, string filter, bool includeSubdirectories)
        {
            if (FPath != path || FFilter != filter || FIncludeSubDirs != includeSubdirectories)
            {
                FPath = path;
                FFilter = filter;
                FIncludeSubDirs = includeSubdirectories;

                Dispose();

                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    FWatcher = null;

                    Changed = Observable.Empty<Path>();
                    Created = Observable.Empty<Path>();
                    Deleted = Observable.Empty<Path>();
                    Renamed = Observable.Empty<RenamedEventArgs>();
                }
                else
                {
                    FWatcher = new FileSystemWatcher();
                    FWatcher.Path = path;
                    FWatcher.Filter = filter;
                    FWatcher.IncludeSubdirectories = includeSubdirectories;

                    Changed = Observable.FromEventPattern<FileSystemEventArgs>(FWatcher, "Changed").Select(e => new Path(e.EventArgs.FullPath));
                    Created = Observable.FromEventPattern<FileSystemEventArgs>(FWatcher, "Created").Select(e => new Path(e.EventArgs.FullPath));
                    Deleted = Observable.FromEventPattern<FileSystemEventArgs>(FWatcher, "Deleted").Select(e => new Path(e.EventArgs.FullPath));
                    Renamed = Observable.FromEventPattern<RenamedEventArgs>(FWatcher, "Renamed").Select(e => e.EventArgs);

                    FWatcher.EnableRaisingEvents = true;
                }
            }
        }

        public IObservable<Path> Changed { get; private set; } = Observable.Empty<Path>();
        public IObservable<Path> Created { get; private set; } = Observable.Empty<Path>();
        public IObservable<Path> Deleted { get; private set; } = Observable.Empty<Path>();
        public IObservable<RenamedEventArgs> Renamed { get; private set; } = Observable.Empty<RenamedEventArgs>();

        public void Dispose()
        {
            FWatcher?.Dispose();
            FWatcher = null;
        }
    }
}
