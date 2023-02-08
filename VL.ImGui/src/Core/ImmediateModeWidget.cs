using Stride.Core.Mathematics;
using System;
using VL.Core;

namespace VL.ImGui.Widgets
{
    [GenerateNode(GenerateImmediate = false)]
    public sealed partial class ImmediateModeWidget : Widget
    {
        public Action<Context>? Updator { get; set; }

        public void Update(Action<Context> updator)
        {
            Updator = updator;
        }

        internal override void UpdateCore(Context context)
        {
            Updator?.Invoke(context);
        }
    }
}
