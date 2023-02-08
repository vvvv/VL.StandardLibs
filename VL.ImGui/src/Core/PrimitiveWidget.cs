using ImGuiNET;
using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets.Primitives
{
    public abstract class PrimitiveWidget : Widget
    {
        internal override sealed void UpdateCore(Context context)
        {
            Draw(context, in context.DrawListPtr, in context.DrawListOffset);
        }

        protected abstract void Draw(Context context, in ImDrawListPtr drawList, in System.Numerics.Vector2 offset);
    }
}
