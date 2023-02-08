namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", GenerateImmediate = false, IsStylable = false)]
    internal partial class SetIDCore : Widget
    {
        public Widget? Input { private get; set; }

        public string? ID { private get; set; }

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.PushID(ID ?? string.Empty);
            try
            {
                context.Update(Input);
            }
            finally
            {
                ImGuiNET.ImGui.PopID();
            }
        }
    }
}
