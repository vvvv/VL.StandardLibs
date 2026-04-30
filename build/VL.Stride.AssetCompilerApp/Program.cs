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

        // Parse command line to extract package repositories and verbose flag early
        var repositoriesArg = args.FirstOrDefault(a => a.StartsWith("--package-repositories=", StringComparison.OrdinalIgnoreCase));
        var repositories = repositoriesArg != null
            ? repositoriesArg.Substring("--package-repositories=".Length).Trim('"')
            : null;

        var verbose = args.Any(a => a.Equals("--verbose", StringComparison.OrdinalIgnoreCase)) ||
                     Environment.GetEnvironmentVariable("VL_STRIDE_COMPILER_VERBOSE") == "1";

        // Setup custom assembly resolution ONLY for VL/Stride assemblies
        var context = new VLAssemblyLoadContext();
        context.SetVerbose(verbose);

        // Extract the package file to determine the project being compiled
        var packageFileArg = args.FirstOrDefault(a => a.StartsWith("--package-file=", StringComparison.OrdinalIgnoreCase));
        var packageFile = packageFileArg != null
            ? packageFileArg.Substring("--package-file=".Length).Trim('"')
            : null;

        context.Initialize(repositories, packageFile);
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
            { "package-repositories=", "Semicolon-separated list of package repository directories", v => { /* Handled in Main */ } },
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
/// Custom assembly load context that resolves assemblies from local packages
/// using .gen.nuspec files to find primary assemblies and their .deps.json files
/// to resolve all runtime dependencies from the global packages folder.
/// Also resolves .NET runtime assemblies like System.Windows.Forms.
/// </summary>
class VLAssemblyLoadContext
{
    private readonly Dictionary<string, string> _assemblyPathCache = new();
    private readonly Dictionary<string, string> _runtimeAssemblyCache = new();
    private readonly List<string> _packageRepositories = new();
    private readonly HashSet<string> _processedDepsJson = new();
    private string? _packageFile;
    private bool _verbose;
    private string? _globalPackagesFolder;

    public void SetVerbose(bool verbose)
    {
        _verbose = verbose;
    }

    public void Initialize(string? repositoriesArg, string? packageFile)
    {
        _packageFile = packageFile;

        // Get global packages folder
        _globalPackagesFolder = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (string.IsNullOrEmpty(_globalPackagesFolder))
        {
            _globalPackagesFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget", "packages");
        }

        // Parse package repositories from command line argument
        if (!string.IsNullOrEmpty(repositoriesArg))
        {
            var repositories = repositoriesArg.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var repository in repositories)
            {
                var trimmedPath = repository.Trim();
                if (Directory.Exists(trimmedPath))
                {
                    _packageRepositories.Add(trimmedPath);
                }
                else if (_verbose)
                {
                    Console.WriteLine($"[VL Asset Compiler] Warning: Repository does not exist: {trimmedPath}");
                }
            }
        }

        // Build runtime assembly cache from .NET installation
        BuildRuntimeAssemblyCache();

        // PRIORITY 1: Process the project's own deps.json first (by finding its .gen.nuspec in repositories)
        if (!string.IsNullOrEmpty(_packageFile))
        {
            TryProcessProjectDepsJson();
        }

        // PRIORITY 2: Build assembly cache from all local packages
        BuildAssemblyCache();

        if (_verbose)
        {
            Console.WriteLine($"[VL Asset Compiler] Found {_assemblyPathCache.Count} assemblies from packages");
            Console.WriteLine($"[VL Asset Compiler] Found {_runtimeAssemblyCache.Count} runtime assemblies");
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

    private void TryProcessProjectDepsJson()
    {
        try
        {
            // Get the package ID from the project file path
            var packageId = Path.GetFileNameWithoutExtension(_packageFile);

            if (_verbose)
                Console.WriteLine($"[VL Asset Compiler] Looking for {packageId}.gen.nuspec in package repositories...");

            // Look for PackageId/PackageId.gen.nuspec in the repositories
            foreach (var repo in _packageRepositories)
            {
                var packageDir = Path.Combine(repo, packageId);
                var nuspecPath = Path.Combine(packageDir, $"{packageId}.gen.nuspec");

                if (File.Exists(nuspecPath))
                {
                    if (_verbose)
                        Console.WriteLine($"[VL Asset Compiler] Found project nuspec (PRIORITY): {nuspecPath}");

                    ProcessProjectNuspec(nuspecPath, packageDir);
                    return;
                }
            }

            if (_verbose)
                Console.WriteLine($"[VL Asset Compiler] Warning: Could not find {packageId}.gen.nuspec in any repository");
        }
        catch (Exception ex)
        {
            if (_verbose)
                Console.WriteLine($"[VL Asset Compiler] Error looking for project deps.json: {ex.Message}");
        }
    }

    private void ProcessProjectNuspec(string nuspecPath, string packageDir)
    {
        try
        {
            var doc = XDocument.Load(nuspecPath);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            // Get package metadata
            var metadata = doc.Root?.Element(ns + "metadata");
            if (metadata == null)
                return;

            var packageId = metadata.Element(ns + "id")?.Value;
            if (string.IsNullOrEmpty(packageId))
                return;

            // Find lib files in the nuspec to locate the primary assembly
            var files = doc.Root?.Element(ns + "files")?.Elements(ns + "file");
            if (files == null)
                return;

            foreach (var file in files)
            {
                var src = file.Attribute("src")?.Value;
                var target = file.Attribute("target")?.Value;

                if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(target))
                    continue;

                // Only process lib assemblies
                if (!target.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) &&
                    !target.StartsWith("lib\\", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!target.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Resolve src path (absolute or relative to package directory)
                var assemblyPath = Path.IsPathRooted(src) ? src : Path.Combine(packageDir, src);
                if (!File.Exists(assemblyPath))
                    continue;

                // Look for accompanying .deps.json file
                var depsJsonPath = Path.ChangeExtension(assemblyPath, ".deps.json");
                if (File.Exists(depsJsonPath))
                {
                    if (_verbose)
                        Console.WriteLine($"[VL Asset Compiler] Found project deps.json via nuspec: {depsJsonPath}");

                    ProcessDepsJson(depsJsonPath);
                    return; // Process only the first matching assembly's deps.json
                }
            }

            if (_verbose)
                Console.WriteLine($"[VL Asset Compiler] Warning: Could not find deps.json for project {packageId}");
        }
        catch (Exception ex)
        {
            if (_verbose)
                Console.WriteLine($"[VL Asset Compiler] Error processing project nuspec {nuspecPath}: {ex.Message}");
        }
    }

    private void BuildAssemblyCache()
    {
        // Scan local repositories for .gen.nuspec files
        foreach (var repo in _packageRepositories)
        {
            ScanRepository(repo);
        }
    }

    private void ScanRepository(string repositoryPath)
    {
        try
        {
            if (!Directory.Exists(repositoryPath))
                return;

            // Look for PACKAGE/PACKAGE.gen.nuspec pattern
            foreach (var packageDir in Directory.GetDirectories(repositoryPath))
            {
                var packageName = Path.GetFileName(packageDir);
                var nuspecPath = Path.Combine(packageDir, $"{packageName}.gen.nuspec");

                if (File.Exists(nuspecPath))
                {
                    ProcessNuspecFile(nuspecPath, packageDir);
                }
            }
        }
        catch (Exception ex)
        {
            if (_verbose)
                Console.WriteLine($"[VL Asset Compiler] Error scanning repository {repositoryPath}: {ex.Message}");
        }
    }

    private void ProcessNuspecFile(string nuspecPath, string packageDir)
    {
        try
        {
            var doc = XDocument.Load(nuspecPath);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            // Get package metadata
            var metadata = doc.Root?.Element(ns + "metadata");
            if (metadata == null)
                return;

            var packageId = metadata.Element(ns + "id")?.Value;
            if (string.IsNullOrEmpty(packageId))
                return;

            // Find lib files in the nuspec
            var files = doc.Root?.Element(ns + "files")?.Elements(ns + "file");
            if (files == null)
                return;

            foreach (var file in files)
            {
                var src = file.Attribute("src")?.Value;
                var target = file.Attribute("target")?.Value;

                if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(target))
                    continue;

                // Only process lib assemblies
                if (!target.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) &&
                    !target.StartsWith("lib\\", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!target.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Resolve src path (absolute or relative to package directory)
                var assemblyPath = Path.IsPathRooted(src) ? src : Path.Combine(packageDir, src);
                if (!File.Exists(assemblyPath))
                    continue;

                // Extract assembly name
                var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);

                // Add to cache
                if (!_assemblyPathCache.ContainsKey(assemblyName))
                {
                    _assemblyPathCache[assemblyName] = assemblyPath;

                    if (_verbose)
                        Console.WriteLine($"[VL Asset Compiler] Registered {assemblyName} -> {assemblyPath}");
                }

                // Look for accompanying .deps.json file
                var depsJsonPath = Path.ChangeExtension(assemblyPath, ".deps.json");
                if (File.Exists(depsJsonPath))
                {
                    ProcessDepsJson(depsJsonPath);
                }
            }
        }
        catch (Exception ex)
        {
            if (_verbose)
                Console.WriteLine($"[VL Asset Compiler] Error processing nuspec {nuspecPath}: {ex.Message}");
        }
    }

    private void ProcessDepsJson(string depsJsonPath)
    {
        try
        {
            if (_processedDepsJson.Contains(depsJsonPath))
                return;

            _processedDepsJson.Add(depsJsonPath);

            if (_verbose)
                Console.WriteLine($"[VL Asset Compiler] Processing deps.json: {depsJsonPath}");

            using var stream = File.OpenRead(depsJsonPath);

            var dependencyContext = new DependencyContextJsonReader().Read(stream);
            if (dependencyContext == null)
                return;

            // Process runtime libraries
            foreach (var runtimeLibrary in dependencyContext.RuntimeLibraries)
            {
                foreach (var assembly in runtimeLibrary.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths))
                {
                    var assemblyName = Path.GetFileNameWithoutExtension(assembly);

                    // Skip if already cached - this respects priority (project deps.json is processed first)
                    if (_assemblyPathCache.ContainsKey(assemblyName))
                    {
                        if (_verbose)
                            Console.WriteLine($"[VL Asset Compiler] Skipping {assemblyName} - already registered (priority)");
                        continue;
                    }

                    // Resolve from global packages folder
                    var packagePath = Path.Combine(
                        _globalPackagesFolder ?? "",
                        runtimeLibrary.Name.ToLowerInvariant(),
                        runtimeLibrary.Version);

                    var fullAssemblyPath = Path.Combine(packagePath, assembly.Replace('/', Path.DirectorySeparatorChar));

                    if (File.Exists(fullAssemblyPath))
                    {
                        _assemblyPathCache[assemblyName] = fullAssemblyPath;

                        if (_verbose)
                            Console.WriteLine($"[VL Asset Compiler] Registered {assemblyName} -> {fullAssemblyPath} (from deps.json)");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (_verbose)
                Console.WriteLine($"[VL Asset Compiler] Error processing deps.json {depsJsonPath}: {ex.Message}");
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

