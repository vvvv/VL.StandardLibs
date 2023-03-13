using ImGuiNET;
using Stride.Core.Mathematics;
using System.Runtime.CompilerServices;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Input (Int3)", Category = "ImGui.Widgets", Tags = "number, updown")]
    [WidgetType(WidgetType.Input)]
    internal partial class InputInt3 : ChannelWidget<Int3>
    {

        public string? Label { get; set; }

        public int Step { private get; set; } = 1;

        public int StepFast { private get; set; } = 100;

        public ImGuiNET.ImGuiInputTextFlags Flags { private get; set; }

        Int3 lastframeValue;

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (InputInt3.CleanedUpAPI(Context.GetLabel(this, Label), ref value, Flags) && value != lastframeValue)
                Value = value;
            lastframeValue = value;
        }


        public static bool CleanedUpAPI(string label, ref Int3 v, ImGuiInputTextFlags flags)
        {
            ref var x = ref v.X;
            if (ImGuiNET.ImGui.InputInt3(label, ref x, flags))
            {
                v = Unsafe.As<int, Int3>(ref x);
                return true;
            }
            return false;
        }
    }
}
