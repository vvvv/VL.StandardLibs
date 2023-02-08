using Stride.Core.Mathematics;
using System.Reactive;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Flexible button behavior without the visuals, frequently useful to build custom behaviors using the public api (along with IsItemActive, IsItemHovered, etc.)
    /// </summary>
    [GenerateNode(Category = "ImGui.Widgets", Button = true, IsStylable = false, Tags = "bang")]
    internal partial class InvisibleButton : ChannelWidget<Unit>
    {
        public string? Label { get; set; }

        public Vector2 Size { private get; set; }

        public ImGuiNET.ImGuiButtonFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            Update();
            if (ImGuiNET.ImGui.InvisibleButton(Context.GetLabel(this, Label), Size.FromHectoToImGui(), Flags))
                Value = Unit.Default;
        }
    }
}
