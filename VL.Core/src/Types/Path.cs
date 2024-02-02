using System;
using System.IO;
using NetPath = System.IO.Path;
using System.Collections.Generic;
using System.Linq;
using VL.Core;
using VL.Lib.Collections;
using VL.Core.Utils;

namespace VL.Lib.IO
{
    [Serializable]
    public partial class Path : IEquatable<Path>
    {
        public static readonly Path Default = new Path(string.Empty);

        private readonly string _path;

        [NonSerialized]
        private Lazy<FileSystemInfo> _info;

        [NonSerialized]
        private long? _size;

        [NonSerialized]
        private Lazy<Path> _parent;

        [NonSerialized]
        private Spread<Path> _children;
        
        public Path(string path)
        {
            _path = path;
        }

        //needed for iobox
         
        public override string ToString()
        {
            return this;
        }

        public Path(Path parent, FileSystemInfo info)
        {
            _path = info.FullName;
            if (parent != null)
                _parent = new Lazy<Path>(() => parent, false);
            _info = new Lazy<FileSystemInfo>(() => info, false);
        }

        public static Path FilePath(string input)
        {
            return new Path(null, new FileInfo(input));
        }

        public static Path DirectoryPath(string input)
        {
            return new Path(null, new DirectoryInfo(input));
        }

        public static implicit operator string(Path p) => p?._path;
        public static explicit operator Path(string s) => s != null ? new Path(s) : null;

        internal FileSystemInfo Info => LazyHelpers.Cache(ref _info, GetInfo);

        FileSystemInfo GetInfo()
        {
            try
            {
                if (File.Exists(_path))
                    return new FileInfo(_path);
                else if (Directory.Exists(_path))
                    return new DirectoryInfo(_path);
                return null;
            }
            catch
            {
                return null;
            }
        }

        FileInfo FileInfo => Info as FileInfo;
        DirectoryInfo DirectoryInfo => Info as DirectoryInfo;

        /// <summary>
        /// Returns whether the path is a file
        /// </summary>
        public bool IsFile
        {
            get
            {
                var fi = FileInfo;
                if (fi != null)
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
                var di = DirectoryInfo;
                if (di != null)
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
        public long Size => LazyHelpers.Cache(ref _size, () => IsFile ? GetFileSize() : GetDirectorySize());

        long GetFileSize() => FileInfo?.Length ?? 0L;

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
        public bool Exists => Info?.Exists ?? false;

        /// <summary>
        /// Updates all properties of the path
        /// </summary>
        /// <returns></returns>
        public Path Refresh() => new Path(_path);

        /// <summary>
        /// For a directory returns its parent directory. For a file returns the directory the file is in
        /// </summary>
        public Path Parent => LazyHelpers.Cache(ref _parent, GetParent);

        Path GetParent()
        {
            if (FileInfo != null)
                return new Path(null, FileInfo.Directory);
            else if (DirectoryInfo != null)
                return new Path(null, DirectoryInfo.Parent);
            else
                return new Path(NetPath.Combine(_path, ".."));
        }

        public Spread<Path> Children => LazyHelpers.Cache(ref _children, () => GetDescendants(includeSubdirectories: true));

        /// <summary>
        /// Returns all files and folders contained withinin a directory
        /// </summary>
        /// <param name="searchPattern"></param>
        /// <param name="includeSubdirectories"></param>
        /// <param name="includeHidden"></param>
        /// <returns></returns>
        public Spread<Path> GetDescendants(string searchPattern = "*.*", bool includeSubdirectories = false, bool includeHidden = false)
        {
            if (DirectoryInfo != null)
                return DirectoryInfo.EnumerateFileSystemInfos(searchPattern, includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                    .Where(i => includeHidden || (!i.Attributes.HasFlag(FileAttributes.Hidden)))
                    .Select(i => new Path(this, i))
                    .ToSpread();
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
            if (DirectoryInfo != null)
            {
                return DirectoryInfo.EnumerateDirectories(searchPattern, includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                    .Where(i => includeHidden || (!i.Attributes.HasFlag(FileAttributes.Hidden)))
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
            if (DirectoryInfo != null)
                return DirectoryInfo.EnumerateFiles(searchPattern, includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                    .Where(i => includeHidden || (!i.Attributes.HasFlag(FileAttributes.Hidden)))
                    .Select(i => new Path(this, i))
                    .ToSpread();
            return Spread<Path>.Empty;
        }

        public string DirectoryName
        {
            get
            {
                return DirectoryInfo?.FullName ?? NetPath.GetDirectoryName(_path);
            }
                
        }

        public string Name
        {
            get
            {
                return Info?.Name ?? NetPath.GetFileNameWithoutExtension(_path);
            }
        }

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
                directory = FileInfo?.DirectoryName ?? NetPath.GetDirectoryName(_path);
                filename = NetPath.GetFileNameWithoutExtension(FileInfo?.Name ?? _path);
                extension = FileInfo?.Extension ?? NetPath.GetExtension(_path);
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
            var attrs = Info?.Attributes ?? FileAttributes.Offline;
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
            if (Info != null)
            {
                var attrs = Info.Attributes;
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
                
                Info.Attributes = attrs;
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
            if (Info != null)
            {
                creationTime = Info.CreationTime;
                lastWriteTime = Info.LastWriteTime;
                lastAccessTime = Info.LastAccessTime;
            }
            else
            {
                creationTime = lastWriteTime = lastAccessTime = DateTime.MinValue;
            }
        }

        public void CreateDirectory(out bool success)
        {
            try
            {
                if (Info == null)
                    _info = new Lazy<FileSystemInfo>(() => new DirectoryInfo(_path), false);
                if (DirectoryInfo != null)
                {
                    DirectoryInfo.Create();
                    success = true;
                }
                else
                    success = false;
            }
            catch
            {
                success = false;
            }
        }

        public Path Move(Path newPath, bool replaceExisting)
        {
            if (Info == null)
                return new Path(newPath);
            else
            {
                try
                {
                    Path p = new Path(null, this.Info);
                    if (!newPath.Exists || replaceExisting)
                    {
                        if (IsFile)
                            p.FileInfo.MoveTo(newPath);
                        else
                            p.DirectoryInfo.MoveTo(newPath);
                    }
                    return p;
                }
                catch
                {
                    return new Path(newPath);
                }
            }
        }

       
        public Path Rename(string newName, bool replaceExisting)
        {
            if (Info != null)
            {
                return Move(new Path(Info.FullName.Replace(Info.Name, newName)), replaceExisting);
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
            if (Info != null)
            {
                if (IsFile)
                {
                    var f = new Path(null, this.Info);
                    var n = new Path(null, new FileInfo(newPath));
                    if (!n.Exists || replaceExisting)
                    {
                        worker.Add(() => 
                        {
                            if (!n.FileInfo.Directory.Exists)
                                n.FileInfo.Directory.Create();
                            f.FileInfo.CopyTo(newPath);
                            return n;
                        });
                        p = n;
                    }
                }
                else
                {
                    var d = new Path(null, new DirectoryInfo(newPath));
                    bool dirExists = d.Exists;
                    if (!dirExists)
                        d.CreateDirectory(out dirExists);
                    if (dirExists)
                    {
                        foreach (var df in Children)
                        {
                            var sub = new List<Func<Path>>();
                            df.CopyTo(df.Info.FullName.Replace(this.DirectoryInfo.FullName, d.DirectoryInfo.FullName), replaceExisting, out sub);
                            worker.AddRange(sub);
                        }
                        p = d;
                    }
                }
            }
            return p;
        }

        public bool Delete()
        {
            if (Info == null)
                return false;
            else
            {
                if (DirectoryInfo != null)
                    DirectoryInfo.Delete(true);
                else
                    Info.Delete();
                return true;
            }
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

            if (NetPath.GetPathRoot(this) != NetPath.GetPathRoot(basePath))
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
        public override int GetHashCode() => Info?.FullName.GetHashCode() ?? _path.GetHashCode();
    }
}


