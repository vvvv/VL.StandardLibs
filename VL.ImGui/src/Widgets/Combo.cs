namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Combo (String)", Category = "ImGui.Widgets", Tags = "dropdown, pulldown, enum")]
    internal partial class Combo : ChannelWidget<string>
    {

        public string? Label { get; set; }

        public IEnumerable<string> Items { get; set; } = Enumerable.Empty<string>();

        public string? Format { private get; set; }

        public ImGuiNET.ImGuiComboFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var value = Update();

            if (Items != null && Items.Any())
            {
                if (ImGuiNET.ImGui.BeginCombo(Context.GetLabel(this, Label), value, Flags))
                {
                    try
                    {
                        foreach (var item in Items)
                        {
                            bool is_selected = value == item;
                            if (ImGuiNET.ImGui.Selectable(item, is_selected))
                            {
                                Value = item;
                            }
                            if (is_selected)
                            {
                                ImGuiNET.ImGui.SetItemDefaultFocus();
                            }
                        }
                    }
                    finally
                    {
                        ImGuiNET.ImGui.EndCombo();
                    }
                }
            }
        }
    }
}
