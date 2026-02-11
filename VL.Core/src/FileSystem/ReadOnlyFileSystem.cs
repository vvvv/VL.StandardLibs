#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace VL.Core;

internal sealed class ReadOnlyFileSystem : IFileSystem
{
    private readonly IFileSystem fileSystem;

    public ReadOnlyFileSystem(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
    }

    public ValueTask<string> CreateDirectoryAsync(string directoryPath)
    {
        throw new NotSupportedException("This file system is read-only and does not support creating directories.");
    }

    public ValueTask DeleteFileAsync(string filePath)
    {
        throw new NotSupportedException("This file system is read-only and does not support deleting files.");
    }

    public IAsyncEnumerable<string> EnumerateFilesAsync(string directory, string pattern, EnumerationOptions options)
    {
        return fileSystem.EnumerateFilesAsync(directory, pattern, options);
    }

    public ValueTask<bool> FileExistsAsync(string filePath)
    {
        return fileSystem.FileExistsAsync(filePath);
    }

    public ValueTask<bool> DirectoryExistsAsync(string directoryPath)
    {
        return fileSystem.DirectoryExistsAsync(directoryPath);
    }

    public string GetLocalPath(string filePath)
    {
        return fileSystem.GetLocalPath(filePath);
    }

    public ValueTask<Stream> OpenAsync(string filePath, FileStreamOptions options)
    {
        if (options.Mode != FileMode.Open || options.Access != FileAccess.Read)
            throw new NotSupportedException("This file system is read-only and only supports opening existing files for reading.");

        return fileSystem.OpenAsync(filePath, options);
    }

    public IObservable<string> Watch(string directory, string filter = "*.*", bool includeSubdirectories = false)
    {
        return fileSystem.Watch(directory, filter, includeSubdirectories);
    }
}
