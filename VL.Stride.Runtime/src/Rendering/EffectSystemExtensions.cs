using Stride.Core.IO;
using Stride.Rendering;
using Stride.Shaders.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using VL.Stride.Core.IO;

namespace VL.Stride.Rendering
{
    static class EffectSystemExtensions
    {
        public static void InstallEffectCompilerWithCustomPaths(this EffectSystem effectSystem)
        {
            var databaseProvider = effectSystem.Services.GetService<IDatabaseFileProviderService>();
            var shaderFileProvider = new ShaderFileProvider(databaseProvider.FileProvider);
            effectSystem.Services.AddService(shaderFileProvider);
            effectSystem.Compiler = EffectCompilerFactory.CreateEffectCompiler(shaderFileProvider, effectSystem, database: databaseProvider.FileProvider);
        }

        public static void EnsurePathIsVisible(this EffectSystem effectSystem, string path)
        {
            var shaderFileProvider = effectSystem.Services.GetService<ShaderFileProvider>();
            if (shaderFileProvider != null)
                shaderFileProvider.Ensure(path);
        }

        public static IVirtualFileProvider GetFileProviderForSpecificPath(this EffectSystem effectSystem, string path)
        {
            var shaderFileProvider = effectSystem.Services.GetService<ShaderFileProvider>();
            if (shaderFileProvider != null && shaderFileProvider.TryGetFileProvider(path, out var fileProvider))
                return fileProvider;
            return null;
        }

        sealed class ShaderFileProvider : AggregateFileProvider
        {
            private readonly Dictionary<string, FileSystemProvider> fileProviders = new(StringComparer.OrdinalIgnoreCase);

            public ShaderFileProvider(params IVirtualFileProvider[] virtualFileProviders)
                : base(virtualFileProviders)
            {
            }

            public void Ensure(string path)
            {
                lock (fileProviders)
                {
                    if (!fileProviders.TryGetValue(path, out var fileProvider))
                    {
                        Add(fileProvider = new FlatFileSystemProvider(rootPath: null, path));
                        fileProviders.Add(path, fileProvider);
                    }
                }
            }

            public bool TryGetFileProvider(string path, out FileSystemProvider fileProvider)
            {
                lock (fileProviders)
                {
                    return fileProviders.TryGetValue(path, out fileProvider);
                }
            }
        }

        // When Stride looks up shaders it assumes a flat folder structure (shaders/NAME.sdsl)
        // This class allows to organize the shaders in subfolders by keeping a mapping of name to file path in a local cache.
        sealed class FlatFileSystemProvider : FileSystemProvider
        {
            private Dictionary<string, string> filesFlat;
            private DateTime lastCheck;

            public FlatFileSystemProvider(string rootPath, string localBasePath) : base(rootPath, localBasePath)
            {
            }

            protected override string ConvertUrlToFullPath(string url)
            {
                var path = base.ConvertUrlToFullPath(url);
                if (url.StartsWith(EffectCompilerBase.DefaultSourceShaderFolder))
                {
                    var fileName = Path.GetFileName(path);
                    if (GetFilesFlat().TryGetValue(fileName, out var filePath))
                        return filePath;
                }
                return path;
            }

            private Dictionary<string, string> GetFilesFlat()
            {
                // Invalidate the cached result after 500ms
                var now = DateTime.Now;
                if (now > lastCheck + TimeSpan.FromMilliseconds(500))
                    this.filesFlat = null;

                var filesFlat = this.filesFlat;
                if (filesFlat is null)
                {
                    filesFlat = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var localBasePath = base.ConvertUrlToFullPath(EffectCompilerBase.DefaultSourceShaderFolder);
                    foreach (var file in Directory.GetFiles(localBasePath, "*", SearchOption.AllDirectories))
                        filesFlat[Path.GetFileName(file)] = file;
                    lastCheck = DateTime.Now;
                    this.filesFlat = filesFlat;
                }

                return filesFlat;
            }
        }
    }
}
