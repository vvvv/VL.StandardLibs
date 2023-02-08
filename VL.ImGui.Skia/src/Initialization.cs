using VL.Core;
using VL.Core.CompilerServices;

[assembly: AssemblyInitializer(typeof(VL.ImGui.Skia.Initialization))]

namespace VL.ImGui.Skia
{
    public sealed class Initialization : AssemblyInitializer<Initialization>
    {
        protected override void RegisterServices(IVLFactory factory)
        {
            NodeBuildingUtils.RegisterGeneratedNodes(factory, "VL.ImGUI.Skia.Nodes", typeof(Initialization).Assembly);
        }
    }
}
