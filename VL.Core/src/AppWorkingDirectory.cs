#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace VL.Core;

internal static class AppWorkingDirectory
{
    private static readonly ConcurrentDictionary<string, string> InstanceDirectories = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<Mutex> InstanceDirectoryMutexes = new();
    private static readonly object MutexListLock = new();

    /// <summary>
    /// Gets an application-specific working directory for storing data.
    /// </summary>
    /// <param name="appVersionOverride">An optional version segment used instead of the entry assembly version.</param>
    /// <param name="usePerInstanceDirectory">
    /// If <see langword="true"/>, reserves and returns a per-process subdirectory so concurrently running instances do not write into the same folder.
    /// </param>
    /// <returns>
    /// The resolved path, or <see langword="null"/> when no suitable base path can be determined.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="appVersionOverride"/> is <see langword="null"/>.</exception>
    public static string? GetWorkingDirectoryForApp(string? appVersionOverride = null, bool usePerInstanceDirectory = false)
    {
        var sharedDirectory = GetSharedWorkingDirectoryForApp(appVersionOverride);
        if (sharedDirectory is null || !usePerInstanceDirectory)
            return sharedDirectory;

        return InstanceDirectories.GetOrAdd(sharedDirectory, AllocatePerInstanceDirectory);
    }

    internal static string GetWorkingDirectoryForVVVVOrApp(string? vvvvVersion)
    {
        var appName = Assembly.GetEntryAssembly()?.GetName().Name;
        var appVersionOverride = appName == "vvvv" || appName == "vvvvc" ? vvvvVersion : null;
        return GetWorkingDirectoryForApp(appVersionOverride, usePerInstanceDirectory: true) ?? Path.Combine(Path.GetTempPath(), appName!);
    }

    private static string? GetSharedWorkingDirectoryForApp(string? appVersionOverride)
    {
        // Check if we have write access to the base directory of the app
        // If yes, use it as it supports the portable case
        // If no, setup a directory in the user profile
        var appBasePath = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (FileSystemUtils.HasWriteAccess(appBasePath))
            return appBasePath;

        var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppDataPath))
            return null;

        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is null)
            return null;

        var appName = entryAssembly.GetName().Name;
        var appVersion = appVersionOverride ?? entryAssembly.GetName().Version?.ToString();

        if (appName != null && appVersion != null)
            return Path.Combine(localAppDataPath, appName, appVersion);
        if (appName != null)
            return Path.Combine(localAppDataPath, appName);

        return null;
    }

    private static string AllocatePerInstanceDirectory(string sharedDirectory)
    {
        Directory.CreateDirectory(sharedDirectory);

        var instanceKey = string.Concat(
            Path.GetFullPath(sharedDirectory),
            "|",
            Environment.ProcessPath ?? Assembly.GetEntryAssembly()?.Location ?? AppContext.BaseDirectory);

        // Try to use the base directory for the first instance (slot -1)
        var baseMutexName = BuildMutexName(instanceKey, -1);
        var baseMutex = new Mutex(initiallyOwned: false, name: baseMutexName);
        if (TryAcquire(baseMutex))
        {
            lock (MutexListLock)
            {
                InstanceDirectoryMutexes.Add(baseMutex);
            }
            return sharedDirectory;
        }
        baseMutex.Dispose();

        // Fall back to numbered instances
        for (var slot = 0; ; slot++)
        {
            var mutexName = BuildMutexName(instanceKey, slot);
            var mutex = new Mutex(initiallyOwned: false, name: mutexName);
            if (!TryAcquire(mutex))
            {
                mutex.Dispose();
                continue;
            }

            lock (MutexListLock)
            {
                InstanceDirectoryMutexes.Add(mutex);
            }

            var directory = Path.Combine(sharedDirectory, $"instance-{slot:D4}");
            Directory.CreateDirectory(directory);
            return directory;
        }
    }

    private static bool TryAcquire(Mutex mutex)
    {
        try
        {
            return mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            return true;
        }
    }

    private static string BuildMutexName(string instanceKey, int slot)
    {
        var keyBytes = Encoding.UTF8.GetBytes($"{instanceKey}|{slot}");
        var hashBytes = SHA256.HashData(keyBytes);
        var hash = Convert.ToHexString(hashBytes);
        return $"Global\\VL-AppWorkingDirectory-{hash}";
    }
}
