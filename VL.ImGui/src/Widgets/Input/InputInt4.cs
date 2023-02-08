using Stride.Core.Mathematics;
using System.Runtime.CompilerServices;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Input (Int4)", Category = "ImGui.Widgets", Tags = "number, updown")]
    [WidgetType(WidgetType.Input)]
    internal partial class InputInt4 : ChannelWidget<Int4>
    {

        public string? Label { get; set; }

        public int Step { private get; set; } = 1;

        public int StepFast { private get; set; } = 100;

        public ImGuiNET.ImGuiInputTextFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var value = Update();

            ref var x = ref value.X;
            if (ImGuiNET.ImGui.InputInt4(Context.GetLabel(this, Label), ref x, Flags))
                Value = Unsafe.As<int, Int4>(ref x);
        }
    }
}
