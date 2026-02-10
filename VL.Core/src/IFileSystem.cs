#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace VL.Core;

// TODO: Move me to VL.Core once satisified
public interface IFileSystem
{
    ValueTask<bool> FileExistsAsync(string filePath);
    ValueTask<Stream> OpenReadAsync(string filePath);
    ValueTask<Stream> OpenWriteAsync(string filePath);
    IAsyncEnumerable<string> EnumerateFilesAsync(string directory, string pattern, SearchOption searchOption);
    IObservable<string> Watch(string directory, string filter = "*.*", bool includeSubdirectories = false);
}

sealed class LocalFileSystem : IFileSystem
{
    public static readonly LocalFileSystem Instance = new LocalFileSystem();

    private LocalFileSystem() { }

    public string GetLocalPath(string filePath) => filePath;
    public ValueTask<bool> FileExistsAsync(string filePath) => ValueTask.FromResult(File.Exists(filePath));
    public ValueTask<Stream> OpenReadAsync(string filePath) => ValueTask.FromResult<Stream>(File.OpenRead(filePath));
    public ValueTask<Stream> OpenWriteAsync(string filePath)
    {
        if (Path.GetDirectoryName(filePath) is { } dir)
            Directory.CreateDirectory(dir);
        return ValueTask.FromResult<Stream>(File.Open(filePath, FileMode.Create, FileAccess.Write));
    }
    public async IAsyncEnumerable<string> EnumerateFilesAsync(string directory, string searchPattern, SearchOption searchOption)
    {
        foreach (var filePath in Directory.EnumerateFiles(directory, searchPattern, searchOption))
        {
            yield return filePath;
        }
    }

    public IObservable<string> Watch(string directory, string filter, bool includeSubdirectories)
    {
        return FileSystemUtils.WatchDir(directory, filter, includeSubdirectories)
            .Select(args => args.FullPath);
    }
}

public sealed class VirtualFileSystem : IFileSystem
{
    private record struct FileSystemEntry(string Prefix, string LocalBasePath, IFileSystem FileSystem)
    {
        public bool IsLocal => FileSystem == LocalFileSystem.Instance;
        public string ToFileSystem(string filePath) => filePath.Substring(Prefix.Length);
        public string FromFileSystem(string filePath) => Prefix + filePath;
        public string ToLocalPath(string filePath) => Path.Combine(LocalBasePath, ToFileSystem(filePath));
    }

    public static VirtualFileSystem Default { get; } = new VirtualFileSystem();

    private List<FileSystemEntry> fileSystems = new List<FileSystemEntry>();

    private VirtualFileSystem() 
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

    public ValueTask<Stream> OpenReadAsync(string filePath)
    {
        ref var entry = ref GetFileSystemEntry(filePath);
        var fileSystemPath = entry.ToFileSystem(filePath);
        return entry.FileSystem.OpenReadAsync(fileSystemPath);
    }

    public ValueTask<Stream> OpenWriteAsync(string filePath)
    {
        ref var entry = ref GetFileSystemEntry(filePath);
        var fileSystemPath = entry.ToFileSystem(filePath);
        return entry.FileSystem.OpenWriteAsync(fileSystemPath);
    }

    public async IAsyncEnumerable<string> EnumerateFilesAsync(string directory, string pattern, SearchOption searchOption)
    {
        var entry = GetFileSystemEntry(directory);
        var fileSystemDirectory = entry.ToFileSystem(directory);
        await foreach (var filePath in entry.FileSystem.EnumerateFilesAsync(fileSystemDirectory, pattern, searchOption))
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
}

public static class FileSystemExtensions
{
    public static async Task<Stream?> OpenReadOrNullAsync(this IFileSystem fileSystem, string filePath)
    {
        try
        {
            return await fileSystem.OpenReadAsync(filePath);
        }
        catch
        {
            return null;
        }
    }
}