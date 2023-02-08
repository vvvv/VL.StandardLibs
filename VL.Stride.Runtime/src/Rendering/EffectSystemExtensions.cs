using Stride.Core.IO;
using Stride.Rendering;
using Stride.Shaders.Compiler;
using System.Collections.Generic;
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

        sealed class ShaderFileProvider : AggregateFileProvider
        {
            private readonly HashSet<string> paths = new HashSet<string>();

            public ShaderFileProvider(params IVirtualFileProvider[] virtualFileProviders)
                : base(virtualFileProviders)
            {
            }

            public void Ensure(string path)
            {
                lock (paths)
                {
                    if (paths.Add(path))
                        Add(new FileSystemProvider(rootPath: null, path));
                }
            }
        }
    }
}
