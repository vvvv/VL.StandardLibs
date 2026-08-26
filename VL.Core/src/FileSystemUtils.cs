#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;

namespace VL.Core
{
    public static class FileSystemUtils
    {
        /// <summary>
        /// Watches a directory and emits file system change events for matching entries.
        /// </summary>
        /// <param name="path">The directory path to watch.</param>
        /// <param name="filter">The search pattern used to match file names.</param>
        /// <param name="includeSubdirectories">Whether to monitor all subdirectories.</param>
        /// <returns>An observable stream of file system events.</returns>
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
                    })
                .Catch(Observable.Empty<FileSystemEventArgs>())
                .Publish()
                .RefCount();
            });
        }
        private static readonly ConcurrentDictionary<(string path, string filter, bool includeSubdirectories), IObservable<FileSystemEventArgs>> watchers = new ConcurrentDictionary<(string path, string filter, bool includeSubdirectories), IObservable<FileSystemEventArgs>>();

        /// <summary>
        /// Determines whether the current process can create files in the specified directory.
        /// </summary>
        /// <param name="directoryPath">The directory path to test.</param>
        /// <returns><see langword="true"/> if write access is available; otherwise, <see langword="false"/>.</returns>
        public static bool HasWriteAccess(string directoryPath)
        {
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(directoryPath))
                    return false;

                // Try to create a temporary file
                string testFile = Path.Combine(directoryPath, Path.GetRandomFileName());
                using (FileStream fs = File.Create(testFile, 1, FileOptions.DeleteOnClose))
                {
                    // File created successfully and will be deleted on close
                }
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Searches for a file in the specified directory and then in its parent directories.
        /// </summary>
        /// <param name="directory">The starting directory for the search.</param>
        /// <param name="fileName">The file name to look for.</param>
        /// <returns>The first matching file path if found; otherwise, <see langword="null"/>.</returns>
        public static string? FindInThisDirectoryOrParents(string directory, string fileName)
        {
            return FindInThisDirectoryOrParents(directory, fileName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        private static string? FindInThisDirectoryOrParents(string directory, string fileName, HashSet<string> visited)
        {
            var current = directory;
            while (!string.IsNullOrEmpty(current))
            {
                if (!visited.Add(current))
                    return default;

                var filePath = Path.Combine(current, fileName);
                if (File.Exists(filePath))
                    return filePath;
                current = Path.GetDirectoryName(current);
            }
            return default;
        }

        /// <summary>
        /// Searches each file's containing directory and its parent directories for the specified file name.
        /// </summary>
        /// <param name="files">The file paths whose containing directories are used as search starting points.</param>
        /// <param name="fileName">The file name to look for.</param>
        /// <returns>A list of matching file paths found across all search paths.</returns>
        public static List<string> FindInContainingDirectories(IEnumerable<string> files, string fileName)
        {
            var results = new List<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in files)
            {
                var directory = Path.GetDirectoryName(file);
                if (directory is null)
                    continue;

                var filePath = FindInThisDirectoryOrParents(directory, fileName, visited);
                if (filePath is not null)
                    results.Add(filePath);
            }
            return results;
        }
    }
}
