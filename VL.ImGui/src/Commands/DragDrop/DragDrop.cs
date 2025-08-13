using ImGuiNET;
using System.Reflection.Emit;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class DragDrop : Widget
    {
        public ImGuiNET.ImGuiDragDropFlags Flags { private get; set; }

        public Func<string> Payload { private get; set; } = () => string.Empty;
        public Func<string> Tooltip { private get; set; } = () => string.Empty;

        internal override void UpdateCore(Context context)
        {
            if (ImGuiNET.ImGui.BeginDragDropSource(Flags))
            {
                var s = Payload?.Invoke();
                if (s != null)
                {
                    var payloadBytes = System.Text.Encoding.UTF8.GetBytes(s);
                    unsafe
                    {
                        fixed (byte* ptr = payloadBytes)
                        {
                            ImGuiNET.ImGui.SetDragDropPayload("DragDropPayload", (IntPtr)ptr, (uint)payloadBytes.Length);
                        }
                    }

                    var tooltip = Tooltip?.Invoke();
                    if (!string.IsNullOrEmpty(tooltip))
                    {
                        ImGuiNET.ImGui.TextUnformatted(tooltip);
                    }
                    ImGuiNET.ImGui.EndDragDropSource();
                }
            }
        }
    }   
}
