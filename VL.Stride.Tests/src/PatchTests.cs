using Microsoft.Extensions.DependencyModel;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using VL.Lang;
using VL.Lang.Symbols;
using VL.Model;

namespace VL.Stride
{
    [TestFixture]
    public class PatchTests
    {
        static PatchTests()
        {
            var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            MainLibPath = Path.GetFullPath(Path.Combine(currentDirectory, @"..\..\..\..\..\"));
        }

        [OneTimeSetUp]
        public void Setup()
        {
            var RepositoriesPath = Path.GetFullPath(Path.Combine(MainLibPath, @"..\..\"));
            var VLPath = Path.GetFullPath(Path.Combine(MainLibPath, @"..\..\..\vvvv50\"));

            var searchPaths = ImmutableArray.CreateBuilder<string>();

            // Add the vvvv_stride\public-vl\VL.Stride\packages folder
            searchPaths.Add(MainLibPath);

            // Add the vvvv_stride\public-vl folder
            searchPaths.Add(RepositoriesPath);

            // Add the vvvv_stride\vvvv50 folder
            searchPaths.Add(VLPath);

            TestEnvironment = new TestEnvironment(DependencyContext.Load(typeof(PatchTests).Assembly), searchPaths);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TestEnvironment.Dispose();
        }

        public static IEnumerable<string> NormalPatches()
        {
            var vlStride = Directory.GetFiles(Path.Combine(MainLibPath, "VL.Stride"), "*.vl", SearchOption.AllDirectories);
            var vlStrideRuntime = Directory.GetFiles(Path.Combine(MainLibPath, "VL.Stride.Runtime"), "*.vl", SearchOption.AllDirectories);
            var vlStrideWindows = Directory.GetFiles(Path.Combine(MainLibPath, "VL.Stride.Windows"), "*.vl", SearchOption.AllDirectories);

            var pathUri = new Uri(MainLibPath, UriKind.Absolute);
            // Yield all your VL docs
            foreach (var file in vlStrideRuntime.Concat(vlStride).Concat(vlStrideWindows))
            {
                // Shows up red on build server - maybe due to super cheap graphics card?
                if (Path.GetFileName(file) == "HowTo Write a Shader.vl" ||
                    Path.GetFileName(file) == "HowTo Create a Custom ShaderFX Node.vl" ||
                    Path.GetFileName(file) == "HowTo Read Pixels of a Texture.vl" ||
                    Path.GetFileName(file) == "HowTo Write into a VertexBuffer in a Compute Shader.vl" ||
                    Path.GetFileName(file) == "HowTo Extract hue, saturation and lightness from a texture.vl" ||
                    Path.GetFileName(file) == "Example GPU Particle System with Mesh.vl" ||
                    Path.GetFileName(file) == "Example Compute Shader Instancing with Oversampling.vl" ||
                    Path.GetFileName(file) == "HowTo Rearrange Channels of a Texture.vl" ||
                    Path.GetFileName(file) == "VL.Stride.Rendering.Temp.vl")
                    continue;

                var fileUri = new Uri(file, UriKind.Absolute);
                yield return Uri.UnescapeDataString(pathUri.MakeRelativeUri(fileUri).ToString()).Replace("/", @"\");
            }
        }



        public TestEnvironment TestEnvironment;
        public static readonly string MainLibPath;

        /// <summary>
        /// Checks if the document comes with compile time errors (e.g. red nodes). Doesn't actually run the patches.
        /// </summary>
        /// <param name="filePath"></param>
        [TestCaseSource(nameof(NormalPatches))]
        public async Task IsntRedAsync(string filePath)
        {
            filePath = Path.Combine(MainLibPath, filePath);

            var document = await TestEnvironment.LoadAndCompileAsync(filePath);

            // Check document structure
            Assert.True(document.IsValid, message: string.Join(", ", document.AllModelErrors));

            // Check dependenices
            foreach (var dep in document.GetDocSymbols().Dependencies)
                Assert.IsFalse(dep.RemoteSymbolSource is Dummy, $"Couldn't find dependency {dep}. Press F6 to build all library projects!");

            // Check all containers and process node definitions, including application entry point
            CheckNodes(document.AllTopLevelDefinitions);
        }

        public static void CheckNodes(IEnumerable<Node> nodes)
        {
            Parallel.ForEach(nodes, definition =>
            {
                var definitionSymbol = definition.GetSymbol() as IDefinitionSymbol;
                if (definitionSymbol is null) return; // Can be null for empty entry points
                var errorMessages = definition.GetSymbolMessages().Where(m => m.Severity == MessageSeverity.Error);
                Assert.That(errorMessages.None(), () => $"{definition}: {string.Join(Environment.NewLine, errorMessages)}");
                Assert.IsFalse(definitionSymbol.IsUnused, $"The symbol of {definition} is marked as unused.");
            });
        }




        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // Running Tests patches not supported yet. We for now can only check for compile time errors (like red nodes)
    }
}
