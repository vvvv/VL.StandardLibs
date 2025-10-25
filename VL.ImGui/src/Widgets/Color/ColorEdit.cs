using Stride.Core.Mathematics;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets", Tags = "rgba, hsv, hsl")]
    [WidgetType(WidgetType.Default)]
    internal partial class ColorEdit : ChannelWidget<Color4>
    {
        public ImGuiNET.ImGuiColorEditFlags Flags { private get; set; } = ImGuiNET.ImGuiColorEditFlags.AlphaBar;

        public ColorEdit()
        {
            Value = Color4.White;
        }

        internal override void UpdateCore(Context context)
        {
            var value = Update().ToImGui();
            if (ImGuiNET.ImGui.ColorEdit4(widgetLabel.Update(label.Value), ref value, Flags))
                Value = value.ToVLColor4();
        }
    }
}
