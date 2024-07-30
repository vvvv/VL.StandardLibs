namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets")]
    internal partial class RadioButton : ChannelWidget<int>
    {
        public int Index { get; set; }

        internal override void UpdateCore(Context context)
        {
            var value = Update();
            if (ImGuiNET.ImGui.RadioButton(widgetLabel.Update(label.Value), ref value, Index))
                Value = value;
        }
    }
}
