#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace VL.Core;

public sealed class VirtualFileSystem : IFileSystem
{
    private record struct FileSystemEntry(string Prefix, string LocalBasePath, IFileSystem FileSystem)
    {
        public bool IsLocal => FileSystem == LocalFileSystem.Instance;
        public string ToFileSystem(string filePath) => filePath.Substring(Prefix.Length);
        public string FromFileSystem(string filePath) => Prefix + filePath;
        public string ToLocalPath(string filePath) => Path.Combine(LocalBasePath, ToFileSystem(filePath));
    }

    private List<FileSystemEntry> fileSystems = new List<FileSystemEntry>();

    public VirtualFileSystem()
    {
        fileSystems.Add(new FileSystemEntry(string.Empty, string.Empty, LocalFileSystem.Instance));
    }

    public void RegisterFileSystem(string prefix, string localBasePath, IFileSystem fileSystem)
    {
        if (string.IsNullOrEmpty(prefix))
            throw new ArgumentException("Prefix cannot be null or empty.", nameof(prefix));
        if (string.IsNullOrEmpty(localBasePath))
            throw new ArgumentException("Local base path cannot be null or empty.", nameof(localBasePath));

        fileSystems.Add(new FileSystemEntry(prefix, localBasePath, fileSystem));
    }

    private ref FileSystemEntry GetFileSystemEntry(string path)
    {
        var fileSystems = CollectionsMarshal.AsSpan(this.fileSystems);
        for (int i = 1; i < fileSystems.Length; i++)
        {
            ref var entry = ref fileSystems[i];
            if (path.StartsWith(entry.Prefix, StringComparison.OrdinalIgnoreCase))
                return ref entry;
        }
        return ref fileSystems[0]; // Local file system is the default
    }

    public bool IsLocalFilePath(string filePath)
    {
        ref var entry = ref GetFileSystemEntry(filePath);
        return entry.IsLocal;
    }

    public string GetLocalPath(string filePath)
    {
        ref var entry = ref GetFileSystemEntry(filePath);
        return entry.ToLocalPath(filePath);
    }

    public ValueTask<bool> FileExistsAsync(string filePath)
    {
        ref var entry = ref GetFileSystemEntry(filePath);
        var fileSystemPath = entry.ToFileSystem(filePath);
        return entry.FileSystem.FileExistsAsync(fileSystemPath);
    }

    public ValueTask<bool> DirectoryExistsAsync(string directoryPath)
    {
        ref var entry = ref GetFileSystemEntry(directoryPath);
        var fileSystemDirectory = entry.ToFileSystem(directoryPath);
        return entry.FileSystem.DirectoryExistsAsync(fileSystemDirectory);
    }

    public ValueTask<Stream> OpenAsync(string filePath, FileStreamOptions options)
    {
        ref var entry = ref GetFileSystemEntry(filePath);
        var fileSystemPath = entry.ToFileSystem(filePath);
        return entry.FileSystem.OpenAsync(fileSystemPath, options);
    }

    public async IAsyncEnumerable<string> EnumerateFilesAsync(string directory, string pattern, EnumerationOptions options)
    {
        var entry = GetFileSystemEntry(directory);
        var fileSystemDirectory = entry.ToFileSystem(directory);
        await foreach (var filePath in entry.FileSystem.EnumerateFilesAsync(fileSystemDirectory, pattern, options))
        {
            yield return entry.FromFileSystem(filePath);
        }
    }

    public IObservable<string> Watch(string directory, string filter = "*.*", bool includeSubdirectories = false)
    {
        var entry = GetFileSystemEntry(directory);
        var fileSystemDirectory = entry.ToFileSystem(directory);
        return entry.FileSystem.Watch(fileSystemDirectory, filter, includeSubdirectories)
            .Select(file => entry.FromFileSystem(file));
    }

    public ValueTask<string> CreateDirectoryAsync(string directoryPath)
    {
        var entry = GetFileSystemEntry(directoryPath);
        var fileSystemDirectory = entry.ToFileSystem(directoryPath);
        return entry.FileSystem.CreateDirectoryAsync(fileSystemDirectory);
    }

    public ValueTask DeleteFileAsync(string filePath)
    {
        var entry = GetFileSystemEntry(filePath);
        var fileSystemPath = entry.ToFileSystem(filePath);
        return entry.FileSystem.DeleteFileAsync(fileSystemPath);
    }
}