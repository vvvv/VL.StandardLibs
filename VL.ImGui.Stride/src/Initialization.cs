using VL.Core;
using VL.Core.CompilerServices;

[assembly: AssemblyInitializer(typeof(VL.ImGui.Stride.Initialization))]

namespace VL.ImGui.Stride
{
    public sealed class Initialization : AssemblyInitializer<Initialization>
    {
        public override void Configure(AppHost appHost)
        {
            NodeBuildingUtils.RegisterGeneratedNodes(appHost, "VL.ImGUI.Stride.Nodes", typeof(Initialization).Assembly);
        }
    }
}
