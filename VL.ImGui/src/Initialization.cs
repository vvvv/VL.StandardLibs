using VL.Core;
using VL.Core.CompilerServices;

[assembly: AssemblyInitializer(typeof(VL.ImGui.Initialization))]

namespace VL.ImGui
{
    public sealed class Initialization : AssemblyInitializer<Initialization>
    {
        protected override void RegisterServices(IVLFactory factory)
        {
            NodeBuildingUtils.RegisterGeneratedNodes(factory, "VL.ImGUI.Nodes", typeof(Initialization).Assembly);
        }
    }
}
