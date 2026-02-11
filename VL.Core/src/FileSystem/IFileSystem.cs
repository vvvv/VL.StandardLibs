#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace VL.Core;

public interface IFileSystem
{
    string GetLocalPath(string filePath) => filePath;
    ValueTask<string> CreateDirectoryAsync(string directoryPath);
    ValueTask<bool> FileExistsAsync(string filePath);
    ValueTask<bool> DirectoryExistsAsync(string directoryPath);
    ValueTask DeleteFileAsync(string filePath);
    ValueTask<Stream> OpenAsync(string filePath, FileStreamOptions options);
    IAsyncEnumerable<string> EnumerateFilesAsync(string directory, string pattern, EnumerationOptions options);
    IObservable<string> Watch(string directory, string filter = "*.*", bool includeSubdirectories = false);
}
