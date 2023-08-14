using VL.Core;
using VL.Core.CompilerServices;

[assembly: AssemblyInitializer(typeof(VL.ImGui.Skia.Initialization))]

namespace VL.ImGui.Skia
{
    public sealed class Initialization : AssemblyInitializer<Initialization>
    {
        public override void Configure(AppHost appHost)
        {
            NodeBuildingUtils.RegisterGeneratedNodes(appHost, "VL.ImGUI.Skia.Nodes", typeof(Initialization).Assembly);
        }
    }
}
