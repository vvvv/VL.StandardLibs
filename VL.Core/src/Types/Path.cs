using System;
using System.IO;
using NetPath = System.IO.Path;
using System.Collections.Generic;
using System.Linq;
using VL.Lib.Collections;

namespace VL.Lib.IO
{
    [Serializable]
    public partial class Path : IEquatable<Path>
    {
        public static readonly Path Default = new Path(string.Empty);

        private readonly string _path;
        
        [NonSerialized]
        private Path _parent;

        public Path(string path)
        {
            _path = path;
        }

        //needed for iobox
         
        public override string ToString()
        {
            return this;
        }

        public Path(Path parent, string path) : this(path)
        {
            _parent = parent;
        }

        public static Path FilePath(string input)
        {
            return new Path(input);
        }

        public static Path DirectoryPath(string input)
        {
            return new Path(input);
        }

        public static implicit operator string(Path p) => p?._path;
        public static explicit operator Path(string s) => s != null ? new Path(s) : null;

        /// <summary>
        /// Returns whether the path is a file
        /// </summary>
        public bool IsFile
        {
            get
            {
                if (File.Exists(_path))
                    return true;
                // Not on disk - distinguish by trailing directory separator
                return !string.IsNullOrWhiteSpace(_path) 
                    && !IsDirectory;
            }
        }

        /// <summary>
        /// Returns whether the path is a folder
        /// </summary>
        public bool IsDirectory
        {
            get
            {
                if (Directory.Exists(_path))
                    return true;
                // Not on disk - distinguish by trailing directory separator
                return !string.IsNullOrWhiteSpace(_path) 
                    && _path.Length > 0 
                    && _path[_path.Length - 1] == NetPath.DirectorySeparatorChar;
            }
        }

        /// <summary>
        /// Whether the path string contains a root.
        /// </summary>
        public bool IsRooted => NetPath.IsPathRooted(_path);

        /// <summary>
        /// Returns the root path (if any).
        /// </summary>
        public Path Root => IsRooted ? new Path(NetPath.GetPathRoot(_path)) : null;

        /// <summary>
        /// Returns the size of a file or all the files in a folder
        /// </summary>
        public long Size => IsFile ? GetFileSize() : GetDirectorySize();

        long GetFileSize() => new FileInfo(_path).Length;

        long GetDirectorySize()
        {
            var result = 0L;
            foreach (var path in Children)
                result += path.Size;
            return result;
        }

        /// <summary>
        /// Returns whether file or folder exists
        /// </summary>
        public bool Exists => File.Exists(_path) || Directory.Exists(_path);

        /// <summary>
        /// For a directory returns its parent directory. For a file returns the directory the file is in
        /// </summary>
        public Path Parent => _parent ??= new Path(NetPath.Combine(_path, ".."));

        public Spread<Path> Children => GetDescendants(includeSubdirectories: false);

        /// <summary>
        /// Returns all files and folders contained withinin a directory
        /// </summary>
        /// <param name="searchPattern"></param>
        /// <param name="includeSubdirectories"></param>
        /// <param name="includeHidden"></param>
        /// <returns></returns>
        public Spread<Path> GetDescendants(string searchPattern = "*.*", bool includeSubdirectories = false, bool includeHidden = false)
        {
            if (IsDirectory)
            {
                var options = GetEnumerationOptions(includeSubdirectories, includeHidden);
                return Directory.EnumerateFileSystemEntries(_path, searchPattern, options)
                    .Select(i => new Path(this, i))
                    .ToSpread();
            }
            return Spread<Path>.Empty;
        }

        /// <summary>
        /// Returns all folders contained within a directory
        /// </summary>
        /// <param name="searchPattern"></param>
        /// <param name="includeSubdirectories"></param>
        /// <param name="includeHidden"></param>
        /// <returns></returns>
        public Spread<Path> GetDirectories(string searchPattern = "*.*", bool includeSubdirectories = false, bool includeHidden = false)
        {
            if (IsDirectory)
            {
                var options = GetEnumerationOptions(includeSubdirectories, includeHidden);
                return Directory.EnumerateDirectories(_path, searchPattern, options)
                    .Select(i => new Path(this, i))
                    .ToSpread();
            }
            return Spread<Path>.Empty;
        }

        /// <summary>
        /// Returns all files contained within a directory
        /// </summary>
        /// <param name="searchPattern"></param>
        /// <param name="includeSubdirectories"></param>
        /// <param name="includeHidden"></param>
        /// <returns></returns>
        public Spread<Path> GetFiles(string searchPattern = "*.*", bool includeSubdirectories = false, bool includeHidden = false)
        {
            if (IsDirectory)
            {
                var options = GetEnumerationOptions(includeSubdirectories, includeHidden);
                return Directory.EnumerateFiles(_path, searchPattern, options)
                    .Select(i => new Path(this, i))
                    .ToSpread();
            }
            return Spread<Path>.Empty;
        }

        private static EnumerationOptions GetEnumerationOptions(bool includeSubdirectories, bool includeHidden)
        {
            return new EnumerationOptions()
            {
                MatchType = MatchType.Win32,
                AttributesToSkip = !includeHidden ? FileAttributes.Hidden : 0,
                IgnoreInaccessible = false,
                RecurseSubdirectories = includeSubdirectories
            };
        }

        public string DirectoryName => NetPath.GetDirectoryName(_path);

        public string Name => NetPath.GetFileNameWithoutExtension(_path);

        public string NameWithExtension => NetPath.GetFileName(_path);

        /// <summary>
        /// Returns the directory path and the name and extension of a file
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filename"></param>
        /// <param name="extension"></param>

        public void Filename(out string directory, out string filename, out string extension)
        {
            if (IsDirectory)
            {
                directory = this;
                filename = string.Empty;
                extension = string.Empty;
            }
            else
            {
                GetFilenameNoThrow(out directory, out filename, out extension);
            }
        }

        [System.Diagnostics.DebuggerStepThrough]
        void GetFilenameNoThrow(out string directory, out string filename, out string extension)
        {
            if (string.IsNullOrWhiteSpace(_path))
            {
                directory = "";
                filename = "";
                extension = "";
                return;
            }

            try
            {
                directory = NetPath.GetDirectoryName(_path);
                filename = NetPath.GetFileNameWithoutExtension(_path);
                extension = NetPath.GetExtension(_path);
                extension = extension.TrimStart(new char[] { '.' });
            }
            catch (Exception)
            {
                directory = "";
                filename = "";
                extension = "";
            }
        }

        /// <summary>
        /// Returns readonly, hidden and system attributes of a file or folder
        /// </summary>
        /// <param name="isReadOnly"></param>
        /// <param name="isHidden"></param>
        /// <param name="isSystem"></param>
        public void GetAttributes(out bool isReadOnly, out bool isHidden, out bool isSystem)
        {
            var attrs = Exists ? File.GetAttributes(_path) : default;
            isReadOnly = attrs.HasFlag(FileAttributes.ReadOnly);
            isHidden = attrs.HasFlag(FileAttributes.Hidden);
            isSystem = attrs.HasFlag(FileAttributes.System);
        }

        /// <summary>
        /// Sets the readonly, hidden and system attributes of a file or folder
        /// </summary>
        /// <param name="isReadOnly"></param>
        /// <param name="isHidden"></param>
        /// <param name="isSystem"></param>
        public void SetAttributes(bool isReadOnly, bool isHidden, bool isSystem)
        {
            if (Exists)
            {
                var attrs = File.GetAttributes(_path);
                if (isReadOnly)
                    attrs |= FileAttributes.ReadOnly;
                else
                    attrs &= ~FileAttributes.ReadOnly;

                if (isHidden)
                    attrs |= FileAttributes.Hidden;
                else
                    attrs &= ~FileAttributes.Hidden;

                if (isSystem)
                    attrs |= FileAttributes.System;
                else
                    attrs &= ~FileAttributes.System;

                File.SetAttributes(_path, attrs);
            }
        }

        /// <summary>
        /// Returns creation date, last write and last access dates of a file or folder
        /// </summary>
        /// <param name="creationTime"></param>
        /// <param name="lastWriteTime"></param>
        /// <param name="lastAccessTime"></param>
        public void Modified(out DateTime creationTime, out DateTime lastWriteTime, out DateTime lastAccessTime)
        {
            var info = GetInfo();
            if (info != null)
            {
                creationTime = info.CreationTime;
                lastWriteTime = info.LastWriteTime;
                lastAccessTime = info.LastAccessTime;
            }
            else
            {
                creationTime = lastWriteTime = lastAccessTime = DateTime.MinValue;
            }
        }

        private FileSystemInfo GetInfo() => IsFile ? new FileInfo(_path) : IsDirectory ? new DirectoryInfo(_path) : null;

        public void CreateDirectory(out bool success)
        {
            try
            {
                Directory.CreateDirectory(_path);
                success = true;
            }
            catch
            {
                success = false;
            }
        }

        public Path Move(Path newPath, bool replaceExisting)
        {
            try
            {
                if (!newPath.Exists || replaceExisting)
                {
                    if (IsFile)
                        File.Move(_path, newPath, replaceExisting);
                    else if (IsDirectory)
                        Directory.Move(_path, newPath);
                }
                return newPath;
            }
            catch
            {
                return newPath;
            }
        }

       
        public Path Rename(string newName, bool replaceExisting)
        {
            if (Exists)
            {
                return Move(new Path(newName), replaceExisting);
            }
            else
            {
                if (!_path.Contains(NetPath.DirectorySeparatorChar))
                    return new Path(newName);
                else
                {
                    var t = _path.Split(new char[] { NetPath.DirectorySeparatorChar, NetPath.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                    return new Path(_path.Replace(t[t.Length - 1], newName));
                }
            }
        }

        public Path CopyTo(string newPath, bool replaceExisting, out List<Func<Path>> worker)
        {
            var p = Default;
            worker = new List<Func<Path>>();
            if (IsFile)
            {
                var n = new Path(newPath);
                if (!n.Exists || replaceExisting)
                {
                    worker.Add(() =>
                    {
                        var newDir = NetPath.GetDirectoryName(newPath);
                        Directory.CreateDirectory(newDir);
                        File.Copy(_path, newPath, replaceExisting);
                        return n;
                    });
                    p = n;
                }
            }
            else if (IsDirectory)
            {
                CopyDirectory(_path, newPath, recursive: true, overwrite: replaceExisting);
                return new Path(newPath);
            }
            return p;

            // https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
            static void CopyDirectory(string sourceDir, string destinationDir, bool recursive, bool overwrite)
            {
                // Get information about the source directory
                var dir = new DirectoryInfo(sourceDir);

                // Check if the source directory exists
                if (!dir.Exists)
                    throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

                // Cache directories before we start copying
                DirectoryInfo[] dirs = dir.GetDirectories();

                // Create the destination directory
                Directory.CreateDirectory(destinationDir);

                // Get the files in the source directory and copy to the destination directory
                foreach (FileInfo file in dir.GetFiles())
                {
                    string targetFilePath = NetPath.Combine(destinationDir, file.Name);
                    file.CopyTo(targetFilePath, overwrite);
                }

                // If recursive and copying subdirectories, recursively call this method
                if (recursive)
                {
                    foreach (DirectoryInfo subDir in dirs)
                    {
                        string newDestinationDir = NetPath.Combine(destinationDir, subDir.Name);
                        CopyDirectory(subDir.FullName, newDestinationDir, true, overwrite);
                    }
                }
            }
        }

        public bool Delete()
        {
            if (IsFile)
            {
                File.Delete(_path);
                return true;
            }
            else if (IsDirectory)
            {
                Directory.Delete(_path, true);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns this absolute path as a relative path to the given base path.
        /// In case the base path has a different root than this path or this path is relative already 
        /// the same path will be returned.
        /// </summary>
        public Path MakeRelative(Path basePath)
        {
            if (basePath == null)
                throw new ArgumentNullException(nameof(basePath));
            if (!basePath.IsRooted)
                throw new ArgumentException($"The base path must be absolute.", nameof(basePath));

            if (!IsRooted)
                return this;

            if (this == basePath)
                return new Path("");

            var baseRoot = basePath.Root[0];
            var thisRoot = Root[0];
            if (char.IsLetter(baseRoot) && char.IsLetter(thisRoot) && baseRoot != thisRoot)
                return this;

            var fromPath = (string)basePath;
            if (fromPath[fromPath.Length - 1] != NetPath.DirectorySeparatorChar)
                fromPath = fromPath + NetPath.DirectorySeparatorChar;
            var toPath = DirectoryName + NetPath.DirectorySeparatorChar;

            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', NetPath.DirectorySeparatorChar);
            if (IsFile)
                relativePath = NetPath.Combine(relativePath, NameWithExtension);
            return new Path(relativePath);
        }

        /// <summary>
        /// Returns this relative path as an absolute path to the given base path.
        /// In case this path is absolute already the same path will be returned.
        /// </summary>
        public Path MakeAbsolute(Path basePath)
        {
            if (basePath == null)
                throw new ArgumentNullException(nameof(basePath));
            if (!basePath.IsRooted)
                throw new ArgumentException($"The base path must be absolute.", nameof(basePath));

            if (IsRooted)
                return this;

            return (Path)NetPath.GetFullPath(basePath + this);
        }

        public static Path operator +(Path a, Path b) => new Path(NetPath.Combine(a, b));

        //IEquatable
        public bool Equals(Path other)
        {
            if (ReferenceEquals(other, null))
                return false;
            return other._path == _path;
        }

        public static bool operator ==(Path a, Path b)
        {
            if (ReferenceEquals(a, b)) return true; // Consider null == null
            if (ReferenceEquals(a, null)) return false;
            return a.Equals(b);
        }

        public static bool operator !=(Path a, Path b) => !(a == b);

        public override bool Equals(object obj) => Equals(obj as Path);
        public override int GetHashCode() => _path != null ? _path.GetHashCode() : 0;
    }
}


