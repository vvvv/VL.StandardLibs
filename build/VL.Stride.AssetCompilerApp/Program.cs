using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyModel;
using Mono.Options;
using Stride.Assets;
using Stride.Assets.Models;
using Stride.Assets.SpriteFont;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.CompilerApp;
using Stride.Core.Assets.Diagnostics;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Particles;
using Stride.Rendering.Materials;
using Stride.Rendering.ProceduralModels;
using Stride.SpriteStudio.Offline;

namespace VL.Stride.AssetCompilerApp;

class Program
{
    static int Main(string[] args)
    {
        // Register MSBuild using Stride's method
        PackageSessionPublicHelper.FindAndSetMSBuildVersion();

        // Parse command line to extract assembly paths and verbose flag early
        var assemblyPathsArg = args.FirstOrDefault(a => a.StartsWith("--assembly-paths=", StringComparison.OrdinalIgnoreCase));
        var assemblyPaths = assemblyPathsArg != null
            ? assemblyPathsArg.Substring("--assembly-paths=".Length).Trim('"')
            : null;

        var verbose = args.Any(a => a.Equals("--verbose", StringComparison.OrdinalIgnoreCase)) ||
                     Environment.GetEnvironmentVariable("VL_STRIDE_COMPILER_VERBOSE") == "1";

        // Setup custom assembly resolution ONLY for VL/Stride assemblies
        var context = new VLAssemblyLoadContext();
        context.SetVerbose(verbose);
        context.Initialize(assemblyPaths);
        AssemblyLoadContext.Default.Resolving += context.ResolveAssembly;

        // Initialize Stride modules (same as PackageBuilderApp does)
        RuntimeHelpers.RunModuleConstructor(typeof(IProceduralModel).Module.ModuleHandle);
        RuntimeHelpers.RunModuleConstructor(typeof(MaterialKeys).Module.ModuleHandle);
        RuntimeHelpers.RunModuleConstructor(typeof(SpriteFontAsset).Module.ModuleHandle);
        RuntimeHelpers.RunModuleConstructor(typeof(ModelAsset).Module.ModuleHandle);
        RuntimeHelpers.RunModuleConstructor(typeof(SpriteStudioAnimationAsset).Module.ModuleHandle);
        RuntimeHelpers.RunModuleConstructor(typeof(ParticleSystem).Module.ModuleHandle);

        // Parse command-line arguments and build
        var result = RunBuilder(args);
        return result;
    }

    static int RunBuilder(string[] args)
    {
        var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
        var showHelp = false;
        var buildEngineLogger = GlobalLogger.GetLogger("BuildEngine");
        var options = new PackageBuilderOptions(new ForwardingLoggerResult(buildEngineLogger));

        var p = new OptionSet
        {
            "VL Stride Asset Compiler (based on Stride Build Tool)",
            string.Format("Usage: {0} inputPackageFile [options]* -b buildPath", exeName),
            string.Empty,
            "=== Options ===",
            string.Empty,
            { "h|help", "Show this message and exit", v => showHelp = v != null },
            { "assembly-paths=", "Semicolon-separated list of assembly paths to resolve dependencies from", v => { /* Handled in Main */ } },
            { "v|verbose", "Show more verbose progress logs", v => options.Verbose = v != null },
            { "d|debug", "Show debug logs (imply verbose)", v => options.Debug = v != null },
            { "log", "Enable file logging", v => options.EnableFileLogging = v != null },
            { "disable-auto-compile", "Disable auto-compile of projects", v => options.DisableAutoCompileProjects = v != null},
            { "project-configuration=", "Project configuration", v => options.ProjectConfiguration = v },
            { "platform=", "Platform name", v => options.Platform = (PlatformType)Enum.Parse(typeof(PlatformType), v) },
            { "solution-file=", "Solution File Name", v => options.SolutionFile = v },
            { "package-id=", "Package Id from the solution file", v => options.PackageId = Guid.Parse(v) },
            { "package-file=", "Input Package File Name", v => options.PackageFile = v },
            { "msbuild-uptodatecheck-filebase=", "BuildUpToDate File base for MSBuild", v => options.MSBuildUpToDateCheckFileBase = v },
            { "o|output-path=", "Output path", v => options.OutputDirectory = v },
            { "b|build-path=", "Build path", v => options.BuildDirectory = v },
            { "log-file=", "Log build in a custom file.", v =>
            {
                options.EnableFileLogging = v != null;
                options.CustomLogFileName = v;
            } },
            { "monitor-pipe=", "Monitor pipe.", v =>
            {
                if (!string.IsNullOrEmpty(v))
                    options.MonitorPipeNames.Add(v);
            } },
            { "slave=", "Slave pipe", v => options.SlavePipe = v },
            { "server=", "This Compiler is launched as a server", v => { } },
            { "t|threads=", "Number of threads to create.", v => options.ThreadCount = int.Parse(v) },
            { "test=", "Run a test session.", v => options.TestName = v },
            { "property:", "Properties. Format is name1=value1;name2=value2", v =>
            {
                if (!string.IsNullOrEmpty(v))
                {
                    foreach (var nameValue in v.Split(new [] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var equalIndex = nameValue.IndexOf('=');
                        if (equalIndex == -1)
                            throw new OptionException("Expect name1=value1;name2=value2 format.", "property");

                        var name = nameValue.Substring(0, equalIndex);
                        var value = nameValue.Substring(equalIndex + 1);
                        if (value != string.Empty)
                            options.Properties.Add(name, value);
                    }
                }
            }
            },
            { "compile-property:", "Compile properties. Format is name1=value1;name2=value2", v =>
            {
                if (!string.IsNullOrEmpty(v))
                {
                    if (options.ExtraCompileProperties == null)
                        options.ExtraCompileProperties = new Dictionary<string, string>();

                    foreach (var nameValue in v.Split(new [] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var equalIndex = nameValue.IndexOf('=');
                        if (equalIndex == -1)
                            throw new OptionException("Expect name1=value1;name2=value2 format.", "property");

                        options.ExtraCompileProperties.Add(nameValue.Substring(0, equalIndex), nameValue.Substring(equalIndex + 1));
                    }
                }
            }
            },
        };

        try
        {
            var unexpectedArgs = p.Parse(args);

            if (showHelp)
            {
                p.WriteOptionDescriptions(Console.Out);
                return 0;
            }

            // Activate proper log level
            buildEngineLogger.ActivateLog(options.LoggerType);

            // Output logs to the console with colored messages
            if (options.SlavePipe == null)
            {
                var consoleLogListener = new ConsoleLogListener { LogMode = ConsoleLogMode.Always };
                GlobalLogger.GlobalMessageLogged += consoleLogListener;
            }

            // Create and run the package builder
            var builder = new PackageBuilder(options);
            var result = builder.Build();

            return (int)result;
        }
        catch (OptionException e)
        {
            Console.Error.WriteLine($"Command option error: {e.Message}");
            return 1;
        }
    }
}

/// <summary>
/// Custom assembly load context that resolves assemblies using explicit assembly paths
/// and their corresponding .deps.json files to resolve all runtime dependencies
/// from the global packages folder.
/// Also resolves .NET runtime assemblies like System.Windows.Forms.
/// </summary>
class VLAssemblyLoadContext
{
    private readonly Dictionary<string, string> _assemblyPathCache = new();
    private readonly Dictionary<string, string> _runtimeAssemblyCache = new();
    private readonly HashSet<string> _processedDepsJson = new();
    private bool _verbose;
    private string? _globalPackagesFolder;

    public void SetVerbose(bool verbose)
    {
        _verbose = verbose;
    }

    public void Initialize(string? assemblyPathsArg)
    {
        // Get global packages folder
        _globalPackagesFolder = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (string.IsNullOrEmpty(_globalPackagesFolder))
        {
            _globalPackagesFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget", "packages");
        }

        // Build runtime assembly cache from .NET installation
        BuildRuntimeAssemblyCache();

        // Process assembly paths from command line
        if (!string.IsNullOrEmpty(assemblyPathsArg))
        {
            var assemblyPaths = assemblyPathsArg.Split(';', StringSplitOptions.RemoveEmptyEntries);

            if (assemblyPaths.Length == 0)
            {
                throw new InvalidOperationException("No assembly paths provided. The build system must pass --assembly-paths with the main assembly and referenced project assemblies.");
            }

            // PRIORITY 1: Process the main assembly (first in the list) and its deps.json
            var mainAssemblyPath = assemblyPaths[0].Trim();
            if (!File.Exists(mainAssemblyPath))
            {
                throw new FileNotFoundException($"Main assembly not found: {mainAssemblyPath}");
            }

            // Register the main assembly directly
            var mainAssemblyName = Path.GetFileNameWithoutExtension(mainAssemblyPath);
            _assemblyPathCache[mainAssemblyName] = mainAssemblyPath;

            if (_verbose)
                Console.WriteLine($"[VL Asset Compiler] Registered main assembly: {mainAssemblyName} -> {mainAssemblyPath}");

            var mainDepsJsonPath = Path.ChangeExtension(mainAssemblyPath, ".deps.json");
            if (!File.Exists(mainDepsJsonPath))
            {
                throw new FileNotFoundException(
                    $"Main assembly deps.json file not found: {mainDepsJsonPath}. " +
                    $"This file is required for dependency resolution. Ensure the project builds successfully.");
            }

            if (_verbose)
                Console.WriteLine($"[VL Asset Compiler] Processing main assembly deps.json: {mainDepsJsonPath}");

            ProcessDepsJson(mainDepsJsonPath, isPrimary: true);

            // PRIORITY 2: Process referenced project assemblies and their deps.json files
            for (int i = 1; i < assemblyPaths.Length; i++)
            {
                var assemblyPath = assemblyPaths[i].Trim();
                if (!File.Exists(assemblyPath))
                {
                    if (_verbose)
                        Console.WriteLine($"[VL Asset Compiler] Warning: Referenced assembly not found: {assemblyPath}");
                    continue;
                }

                // Register the referenced assembly directly
                var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
                if (!_assemblyPathCache.ContainsKey(assemblyName))
                {
                    _assemblyPathCache[assemblyName] = assemblyPath;

                    if (_verbose)
                        Console.WriteLine($"[VL Asset Compiler] Registered referenced assembly: {assemblyName} -> {assemblyPath}");
                }

                var depsJsonPath = Path.ChangeExtension(assemblyPath, ".deps.json");
                if (File.Exists(depsJsonPath))
                {
                    if (_verbose)
                        Console.WriteLine($"[VL Asset Compiler] Processing referenced assembly deps.json: {depsJsonPath}");

                    ProcessDepsJson(depsJsonPath, isPrimary: false);
                }
                else if (_verbose)
                {
                    Console.WriteLine($"[VL Asset Compiler] Warning: deps.json not found for {assemblyPath}");
                }
            }
        }
        else
        {
            throw new InvalidOperationException(
                "--assembly-paths argument is required. The build system must pass the main assembly " +
                "and all referenced project assemblies.");
        }

        if (_verbose)
        {
            Console.WriteLine($"[VL Asset Compiler] Total assemblies registered: {_assemblyPathCache.Count}");
            Console.WriteLine($"[VL Asset Compiler] Runtime assemblies available: {_runtimeAssemblyCache.Count}");
        }
    }

    private void BuildRuntimeAssemblyCache()
    {
        try
        {
            // Get the runtime directory from the current process (e.g., C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.x)
            var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
            if (string.IsNullOrEmpty(runtimeDirectory))
                return;

            var sharedDirectory = Path.GetDirectoryName(runtimeDirectory);
            if (string.IsNullOrEmpty(sharedDirectory))
                return;

            var versionFolder = Path.GetFileName(runtimeDirectory);

            if (_verbose)
                Console.WriteLine($"[VL Asset Compiler] Scanning additional runtime packs for version {versionFolder}");

            // Scan additional runtime packs that our app doesn't directly reference
            // but user code might need (like System.Windows.Forms, ASP.NET Core, etc.)
            var additionalRuntimes = new[]
            {
                "Microsoft.WindowsDesktop.App",
                "Microsoft.AspNetCore.App"
            };

            foreach (var runtimeName in additionalRuntimes)
            {
                var runtimePath = Path.Combine(sharedDirectory, "..", runtimeName, versionFolder);
                runtimePath = Path.GetFullPath(runtimePath);

                if (Directory.Exists(runtimePath))
                {
                    if (_verbose)
                        Console.WriteLine($"[VL Asset Compiler] Found runtime pack: {runtimePath}");

                    foreach (var dllPath in Directory.GetFiles(runtimePath, "*.dll"))
                    {
                        var assemblyName = Path.GetFileNameWithoutExtension(dllPath);
                        if (!_runtimeAssemblyCache.ContainsKey(assemblyName))
                        {
                            _runtimeAssemblyCache[assemblyName] = dllPath;
                        }
                    }
                }
            }

            if (_verbose && _runtimeAssemblyCache.Count > 0)
            {
                Console.WriteLine($"[VL Asset Compiler] Registered {_runtimeAssemblyCache.Count} assemblies from additional runtime packs");
            }
        }
        catch (Exception ex)
        {
            if (_verbose)
                Console.WriteLine($"[VL Asset Compiler] Error building runtime assembly cache: {ex.Message}");
        }
    }

    private void ProcessDepsJson(string depsJsonPath, bool isPrimary)
    {
        if (_processedDepsJson.Contains(depsJsonPath))
            return;

        _processedDepsJson.Add(depsJsonPath);

        if (_verbose)
            Console.WriteLine($"[VL Asset Compiler] Processing deps.json{(isPrimary ? " (PRIMARY)" : "")}: {depsJsonPath}");

        try
        {
            using var stream = File.OpenRead(depsJsonPath);

            var dependencyContext = new DependencyContextJsonReader().Read(stream);
            if (dependencyContext == null)
            {
                var errorMsg = $"Failed to parse deps.json file: {depsJsonPath}";
                if (isPrimary)
                    throw new InvalidOperationException(errorMsg);
                else if (_verbose)
                    Console.WriteLine($"[VL Asset Compiler] Warning: {errorMsg}");
                return;
            }

            // Process runtime libraries
            foreach (var runtimeLibrary in dependencyContext.RuntimeLibraries)
            {
                foreach (var assembly in runtimeLibrary.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths))
                {
                    var assemblyName = Path.GetFileNameWithoutExtension(assembly);

                    // Skip if already cached - this respects priority (primary deps.json is processed first)
                    if (_assemblyPathCache.ContainsKey(assemblyName))
                    {
                        if (_verbose)
                            Console.WriteLine($"[VL Asset Compiler] Skipping {assemblyName} - already registered (priority)");
                        continue;
                    }

                    // Handle based on library type
                    string? foundPath = null;

                    if (runtimeLibrary.Type.Equals("reference", StringComparison.OrdinalIgnoreCase))
                    {
                        // "reference" type means direct assembly reference - look next to the deps.json file
                        var depsDir = Path.GetDirectoryName(depsJsonPath);
                        if (!string.IsNullOrEmpty(depsDir))
                        {
                            var localAssemblyPath = Path.Combine(depsDir, Path.GetFileName(assembly));
                            if (File.Exists(localAssemblyPath))
                            {
                                foundPath = localAssemblyPath;
                                if (_verbose)
                                    Console.WriteLine($"[VL Asset Compiler] Found reference assembly {assemblyName} next to deps.json: {foundPath}");
                            }
                            else if (_verbose)
                            {
                                Console.WriteLine($"[VL Asset Compiler] Warning: Reference assembly not found: {localAssemblyPath}");
                            }
                        }

                        // Skip the NuGet package resolution for reference types
                        if (foundPath != null)
                        {
                            _assemblyPathCache[assemblyName] = foundPath;
                            if (_verbose)
                                Console.WriteLine($"[VL Asset Compiler] Registered {assemblyName} -> {foundPath} (reference)");
                        }
                        continue;
                    }

                    // For package types, resolve from global packages folder
                    var packagePath = Path.Combine(
                        _globalPackagesFolder ?? "",
                        runtimeLibrary.Name.ToLowerInvariant(),
                        runtimeLibrary.Version);

                    var fullAssemblyPath = Path.Combine(packagePath, assembly.Replace('/', Path.DirectorySeparatorChar));

                    if (File.Exists(fullAssemblyPath))
                    {
                        foundPath = fullAssemblyPath;
                        _assemblyPathCache[assemblyName] = foundPath;

                        if (_verbose)
                            Console.WriteLine($"[VL Asset Compiler] Registered {assemblyName} -> {foundPath} (from deps.json)");
                    }
                    else if (_verbose)
                    {
                        Console.WriteLine($"[VL Asset Compiler] Warning: Assembly not found in global packages: {fullAssemblyPath}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error processing deps.json {depsJsonPath}: {ex.Message}";
            if (isPrimary)
                throw new InvalidOperationException(errorMsg, ex);
            else if (_verbose)
                Console.WriteLine($"[VL Asset Compiler] Warning: {errorMsg}");
        }
    }

    public Assembly? ResolveAssembly(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        var name = assemblyName.Name;

        if (name == null)
            return null;

        // Don't touch core framework assemblies - let default resolution handle them
        if (
            name.StartsWith("netstandard") ||
            name.StartsWith("mscorlib") ||
            name.StartsWith("MSBuild"))
        {
            return null;
        }

        // First check our package cache
        if (_assemblyPathCache.TryGetValue(name, out var assemblyPath))
        {
            try
            {
                if (_verbose)
                    Console.WriteLine($"[VL Asset Compiler] Loading {name} from {assemblyPath}");
                return context.LoadFromAssemblyPath(assemblyPath);
            }
            catch (Exception ex)
            {
                if (_verbose)
                    Console.WriteLine($"[VL Asset Compiler] Failed to load from {assemblyPath}: {ex.Message}");
            }
        }

        // Then check runtime assemblies (System.*, Microsoft.*, etc.)
        if (_runtimeAssemblyCache.TryGetValue(name, out var runtimePath))
        {
            try
            {
                if (_verbose)
                    Console.WriteLine($"[VL Asset Compiler] Loading {name} from runtime: {runtimePath}");
                return context.LoadFromAssemblyPath(runtimePath);
            }
            catch (Exception ex)
            {
                if (_verbose)
                    Console.WriteLine($"[VL Asset Compiler] Failed to load runtime assembly from {runtimePath}: {ex.Message}");
            }
        }

        // Let default resolution handle it
        return null;
    }
}

