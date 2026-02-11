#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace VL.Core;

public static class FileSystemExtensions
{
    private static readonly FileStreamOptions s_readOptions = new FileStreamOptions()
    {
        Mode = FileMode.Open,
        Access = FileAccess.Read,
        Share = FileShare.Read
    };

    private static readonly FileStreamOptions s_writeOptions = new FileStreamOptions()
    {
        Mode = FileMode.Create,
        Access = FileAccess.Write,
        Share = FileShare.None
    };

    public static IFileSystem AsReadOnly(this IFileSystem fileSystem)
    {
        return new ReadOnlyFileSystem(fileSystem);
    }

    public static bool IsLocalFilePath(this IFileSystem fileSystem, string filePath)
    {
        return fileSystem.GetLocalPath(filePath) == filePath;
    }

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

    public static ValueTask<Stream> OpenReadAsync(this IFileSystem fileSystem, string filePath)
    {
        return fileSystem.OpenAsync(filePath, s_readOptions);
    }

    public static async ValueTask<Stream> OpenWriteAsync(this IFileSystem fileSystem, string filePath)
    {
        if (Path.GetDirectoryName(filePath) is { } directory)
            await fileSystem.CreateDirectoryAsync(directory);
        return await fileSystem.OpenAsync(filePath, s_writeOptions);
    }

    public static bool FileExists(this IFileSystem fileSystem, string filePath)
    {
        if (fileSystem.IsLocalFilePath(filePath))
            return fileSystem.FileExistsAsync(filePath).Result;

        return Task.Run(() => fileSystem.FileExistsAsync(filePath).AsTask()).Result;
    }

    public static bool DirectoryExists(this IFileSystem fileSystem, string directoryPath)
    {
        if (fileSystem.IsLocalFilePath(directoryPath))
            return fileSystem.DirectoryExistsAsync(directoryPath).Result;
        return Task.Run(() => fileSystem.DirectoryExistsAsync(directoryPath).AsTask()).Result;
    }

    public static void DeleteFile(this IFileSystem fileSystem, string filePath)
    {
            if (fileSystem.IsLocalFilePath(filePath))
                fileSystem.DeleteFileAsync(filePath);
            else
                Task.Run(() => fileSystem.DeleteFileAsync(filePath).AsTask()).Wait();
    }

    public static void CreateDirectory(this IFileSystem fileSystem, string directoryPath)
    {
        if (fileSystem.IsLocalFilePath(directoryPath))
            fileSystem.CreateDirectoryAsync(directoryPath);
        else
            Task.Run(() => fileSystem.CreateDirectoryAsync(directoryPath).AsTask()).Wait();
    }

    public static Stream Open(this IFileSystem fileSystem, string filePath, FileMode mode, FileAccess access, FileShare share)
    {
        var options = new FileStreamOptions()
        {
            Mode = mode,
            Access = access,
            Share = share
        };
        if (fileSystem.IsLocalFilePath(filePath))
            return fileSystem.OpenAsync(filePath, options).Result;
        else
            return Task.Run(() => fileSystem.OpenAsync(filePath, options).AsTask()).Result;
    }

    public static IEnumerable<string> EnumerateFiles(this IFileSystem fileSystem, string directoryPath, string searchPattern = "*", bool recurseSubdirectories = false, bool includeHidden = false)
    {
        var options = new EnumerationOptions()
        {
            RecurseSubdirectories = recurseSubdirectories,
            AttributesToSkip = includeHidden ? FileAttributes.System : FileAttributes.System | FileAttributes.Hidden
        };
        return fileSystem.EnumerateFilesAsync(directoryPath, searchPattern, options).ToBlockingEnumerable();
    }
}