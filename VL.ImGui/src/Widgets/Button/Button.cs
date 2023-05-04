using Stride.Core.Mathematics;
using System.Reactive;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets", Button = true, Tags = "bang")]
    internal partial class Button : ChannelWidget<Unit>, IHasLabel
    {
        public string? Label { get; set; }

        public Vector2 Size { private get; set; }

        internal override void UpdateCore(Context context)
        {
            Update();
            if (ImGuiNET.ImGui.Button(Context.GetLabel(this, Label), Size.FromHectoToImGui()))
                Value = Unit.Default;
        }
    }
}
