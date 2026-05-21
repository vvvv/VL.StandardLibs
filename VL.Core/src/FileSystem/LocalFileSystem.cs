#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace VL.Core;

sealed class LocalFileSystem : IFileSystem
{
    public static readonly LocalFileSystem Instance = new LocalFileSystem();

    private LocalFileSystem() { }

    public ValueTask<bool> FileExistsAsync(string filePath)
    {
        return ValueTask.FromResult(File.Exists(filePath));
    }

    public ValueTask<bool> DirectoryExistsAsync(string directoryPath)
    {
        return ValueTask.FromResult(Directory.Exists(directoryPath));
    }

    public ValueTask<Stream> OpenAsync(string filePath, FileStreamOptions options)
    {
        return ValueTask.FromResult<Stream>(File.Open(filePath, options));
    }

    public async IAsyncEnumerable<string> EnumerateFilesAsync(string directory, string searchPattern, EnumerationOptions options)
    {
        foreach (var filePath in Directory.EnumerateFiles(directory, searchPattern, options))
        {
            yield return filePath;
        }
    }

    public IObservable<string> Watch(string directory, string filter, bool includeSubdirectories)
    {
        return FileSystemUtils.WatchDir(directory, filter, includeSubdirectories)
            .Select(args => args.FullPath);
    }

    public ValueTask<string> CreateDirectoryAsync(string directoryPath)
    {
        return ValueTask.FromResult(Directory.CreateDirectory(directoryPath).FullName);
    }

    public ValueTask DeleteFileAsync(string filePath)
    {
        File.Delete(filePath);
        return ValueTask.CompletedTask;
    }
}
