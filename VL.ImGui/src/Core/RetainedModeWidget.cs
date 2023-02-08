using Stride.Core.Mathematics;
using System;
using VL.Core;

namespace VL.ImGui.Widgets
{
    [GenerateNode(GenerateRetained = false, Category = "ImGui.Internal")]
    public sealed partial class RetainedMode : Widget
    {
        public Widget? Widget { get; set; }

        internal override void UpdateCore(Context context)
        {
            context.Update(Widget);
        }
    }
}
