using Microsoft.Extensions.DependencyInjection;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Stride;

[assembly: AssemblyInitializer(typeof(VL.ImGui.Stride.Initialization))]

namespace VL.ImGui.Stride
{
    public sealed class Initialization : AssemblyInitializer<Initialization>
    {
        public override void CollectDependencies(DependencyCollector collector)
        {
            // We need to ensure Stride related services are initialized
            collector.AddDependency(VL.Stride.Core.Initialization.Default);
            base.CollectDependencies(collector);
        }

        public override void Configure(AppHost appHost)
        {
            NodeBuildingUtils.RegisterGeneratedNodes(appHost, "VL.ImGUI.Stride.Nodes", typeof(Initialization).Assembly);

            var assemblyDir = Path.GetDirectoryName(typeof(Initialization).Assembly.Location);
            if (assemblyDir != null)
            {
                var bundleFile = Path.Combine(
                    assemblyDir, "data", "db", "bundles", "VL.ImGui.Stride.bundle");
                var bundleLoader = appHost.Services.GetRequiredService<BundleLoader>();
                bundleLoader.LoadBundle(bundleFile);
            }
        }
    }
}
