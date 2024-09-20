using Stride.Core.Mathematics;
using System.Reactive;
using VL.Lib.Reactive;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets", Button = true, Tags = "bang")]
    internal partial class Button : ChannelWidget<Bang>
    {
        public Vector2 Size { private get; set; }

        internal override void UpdateCore(Context context)
        {
            Update();
            if (ImGuiNET.ImGui.Button(widgetLabel.Update(label.Value), Size.FromHectoToImGui()))
                Value = Lib.Reactive.Bang.Trigger;
        }
    }
}
