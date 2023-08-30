using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Shaders.Compiler;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using VL.Core;
using VL.Model;

using ServiceRegistry = global::Stride.Core.ServiceRegistry;

namespace VL.Stride.Rendering
{
    static partial class EffectShaderNodes
    {
        public static NodeBuilding.FactoryImpl Init(ServiceRegistry serviceRegistry, IVLNodeDescriptionFactory factory)
        {
            ShaderMetadata.RegisterAdditionalShaderAttributes();

            return new(GetNodeDescriptions(serviceRegistry, factory), forPath: path => factory =>
            {
                // In case "shaders" directory gets added or deleted invalidate the whole factory
                var invalidated = FileSystemUtils.WatchDir(path)
                    .Where(e => (e.ChangeType == WatcherChangeTypes.Created || e.ChangeType == WatcherChangeTypes.Deleted || e.ChangeType == WatcherChangeTypes.Renamed) && e.Name == EffectCompilerBase.DefaultSourceShaderFolder);

                // File provider crashes if directory doesn't exist :/
                var shadersPath = Path.Combine(path, EffectCompilerBase.DefaultSourceShaderFolder);
                if (Directory.Exists(shadersPath))
                {
                    try
                    {
                        var nodes = GetNodeDescriptions(serviceRegistry, factory, path, shadersPath);
                        // Additionaly watch out for new/deleted/renamed files
                        invalidated = invalidated.Merge(FileSystemUtils.WatchDir(shadersPath, includeSubdirectories: true)
                            .Where(e => IsNewOrDeletedShaderFile(e)));
                        return NodeBuilding.NewFactoryImpl(nodes.ToImmutableArray(), invalidated,
                            export: c =>
                            {
                                // Copy all shaders to the project directory but do so only once per shader path relying on the assumption that the generated project
                                // containing the Assets folder will be referenced by projects further up in the dependency tree.
                                var pathExportedKey = (typeof(EffectShaderNodes), shadersPath);
                                if (c.SolutionWideStorage.TryAdd(pathExportedKey, pathExportedKey))
                                {
                                    var assetsFolder = Path.Combine(c.DirectoryPath, "Assets");
                                    Directory.CreateDirectory(assetsFolder);
                                    foreach (var f in Directory.EnumerateFiles(shadersPath))
                                    {
                                        if (string.Equals(Path.GetExtension(f), ".sdsl", StringComparison.OrdinalIgnoreCase) || string.Equals(Path.GetExtension(f), ".sdfx", StringComparison.OrdinalIgnoreCase))
                                        {
                                            var fileName = Path.GetFileName(f);
                                            var shaderExportedKey = (typeof(EffectShaderNodes), fileName);
                                            if (c.SolutionWideStorage.TryAdd(shaderExportedKey, shadersPath))
                                                File.Copy(f, Path.Combine(assetsFolder, fileName), overwrite: true);
                                            else if (c.SolutionWideStorage.TryGetValue(shaderExportedKey, out var x) && x is string existingPath)
                                                throw new InvalidOperationException($"The shader {fileName} ({shadersPath}) has already been copied from a different location ({existingPath}). Make sure shader names are unique.)");
                                        }
                                    }
                                }
                            });
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // When deleting a folder we can run into this one
                    }
                }

                // Just watch for changes
                return NodeBuilding.NewFactoryImpl(invalidated: invalidated);
            });
        }

        static IEnumerable<IVLNodeDescription> GetNodeDescriptions(ServiceRegistry serviceRegistry, IVLNodeDescriptionFactory factory, string path = default, string shadersPath = default)
        {
            var graphicsDeviceService = serviceRegistry.GetService<IGraphicsDeviceService>();
            var graphicsDevice = graphicsDeviceService.GraphicsDevice;
            var contentManager = serviceRegistry.GetService<ContentManager>();
            var effectSystem = serviceRegistry.GetService<EffectSystem>();

            // Ensure path is visible to the effect system
            if (path != null)
                effectSystem.EnsurePathIsVisible(path);

            // Effect system deals with its internal cache on update, so make sure its called.
            effectSystem.Update(default);

            const string sdslFileFilter = "*.sdsl";
            const string drawFXSuffix = "_DrawFX";
            const string computeFXSuffix = "_ComputeFX";
            const string textureFXSuffix = "_TextureFX";
            const string shaderFXSuffix = "_ShaderFX";

            // Traverse either the "shaders" folder in the database or in the given path (if present)
            IVirtualFileProvider fileProvider = default;
            var dbFileProvider = effectSystem.FileProvider; //should include current path
            var sourceManager = dbFileProvider.GetShaderSourceManager();
            if (path != null)
                fileProvider = effectSystem.GetFileProviderForSpecificPath(path);
            else
                fileProvider = contentManager.FileProvider;


            EffectUtils.ResetParserCache();

            foreach (var file in fileProvider.ListFiles(EffectCompilerBase.DefaultSourceShaderFolder, sdslFileFilter, VirtualSearchOption.AllDirectories))
            {
                var effectName = Path.GetFileNameWithoutExtension(file);
                if (effectName.EndsWith(drawFXSuffix))
                {
                    // Shader only for now
                    var name = GetNodeName(effectName, drawFXSuffix);
                    var shaderNodeName = new NameAndVersion($"{name.NamePart}Shader", name.VersionPart);
                    var shaderMetadata = ShaderMetadata.CreateMetadata(effectName, dbFileProvider, sourceManager);

                    yield return factory.NewDrawEffectShaderNode(
                        shaderNodeName, 
                        effectName, 
                        shaderMetadata, 
                        TrackChanges(effectName, shaderMetadata),
                        serviceRegistry,
                        graphicsDevice);
                    //DrawFX node
                }
                else if (effectName.EndsWith(textureFXSuffix))
                {
                    var name = GetNodeName(effectName, textureFXSuffix);
                    var shaderNodeName = new NameAndVersion($"{name.NamePart}Shader", name.VersionPart);
                    var shaderMetadata = ShaderMetadata.CreateMetadata(effectName, dbFileProvider, sourceManager);

                    var shaderNodeDescription = factory.NewImageEffectShaderNode(
                        shaderNodeName, 
                        effectName, 
                        shaderMetadata,
                        TrackChanges(effectName, shaderMetadata),
                        serviceRegistry,
                        graphicsDevice);

                    yield return shaderNodeDescription;

                    yield return factory.NewTextureFXNode(shaderNodeDescription, name, shaderMetadata);
                }
                else if (effectName.EndsWith(computeFXSuffix))
                {
                    // Shader only for now
                    var name = GetNodeName(effectName, computeFXSuffix);
                    var shaderNodeName = new NameAndVersion($"{name.NamePart}Shader", name.VersionPart);
                    var shaderMetadata = ShaderMetadata.CreateMetadata(effectName, dbFileProvider, sourceManager);

                    yield return factory.NewComputeEffectShaderNode(
                        shaderNodeName, 
                        effectName, 
                        shaderMetadata, 
                        TrackChanges(effectName, shaderMetadata),
                        serviceRegistry,
                        graphicsDevice);
                    //ComputeFX node
                }
                else if (effectName.EndsWith(shaderFXSuffix))
                {
                    // Shader only
                    var name = GetNodeName(effectName, shaderFXSuffix);
                    var shaderNodeName = new NameAndVersion($"{name.NamePart}", name.VersionPart);
                    var shaderMetadata = ShaderMetadata.CreateMetadata(effectName, dbFileProvider, sourceManager);

                    yield return factory.NewShaderFXNode( 
                        shaderNodeName, 
                        effectName, 
                        shaderMetadata, 
                        TrackChanges(effectName, shaderMetadata), 
                        serviceRegistry,
                        graphicsDevice);
                }
            }

            // build an observable to track the file changes, also the files of the base shaders
            IObservable<object> TrackChanges(string shaderName, ShaderMetadata shaderMetadata)
            {
                if (shaderMetadata?.FilePath is null)
                    return null;

                var watchNames = new HashSet<string>() { shaderName };

                foreach (var baseClass in shaderMetadata.ParsedShader?.BaseShaders ?? Enumerable.Empty<ParsedShader>())
                {
                    var baseClassPath = baseClass.Shader.Span.Location.FileSource;
                    if (baseClassPath.ToLowerInvariant().Contains("/stride."))
                        continue; //in stride package folder

                    watchNames.Add(Path.GetFileNameWithoutExtension(baseClassPath));
                }

                // Setup our own watcher as Stride doesn't track shaders with errors
                var shadersPath = Path.GetDirectoryName(shaderMetadata.FilePath);
                return NodeBuilding.WatchDir(shadersPath)
                    .Select(e => Path.GetFileNameWithoutExtension(e.Name))
                    .Where(n => watchNames.Contains(n))
                    .Do(n =>
                    {
                        ((EffectCompilerBase)effectSystem.Compiler).ResetCache(new HashSet<string>() { n });
                        foreach (var watchName in watchNames)
                        {
                            EffectUtils.ResetParserCache(watchName);
                        }
                    });
            }
        }

        private static ParameterPinDescription CreatePinDescription(in ParameterKeyInfo keyInfo, HashSet<string> usedNames, ShaderMetadata shaderMetadata, string name = null, bool? isOptionalOverride = default)
        {
            return CreatePinDescription(keyInfo.Key, keyInfo.Count, usedNames, shaderMetadata, name, isOptionalOverride);
        }

        private static ParameterPinDescription CreatePinDescription(ParameterKey key, int count, HashSet<string> usedNames, ShaderMetadata shaderMetadata, string name = null, bool? isOptionalOverride = default)
        {
            var typeInPatch = shaderMetadata.GetPinType(key, out var runtimeDefaultValue, out var compilationDefaultValue);
            shaderMetadata.GetPinDocuAndVisibility(key, out var summary, out var remarks, out var isOptional);

            if (isOptionalOverride.HasValue)
                isOptional |= isOptionalOverride.Value;

            return new ParameterPinDescription(usedNames, key, count,
                compilationDefaultValue: compilationDefaultValue,
                name: name,
                typeInPatch: typeInPatch,
                runtimeDefaultValue: runtimeDefaultValue)
            {
                IsVisible = !isOptional,
                Summary = summary,
                Remarks = remarks
            };
        }
    }
}
