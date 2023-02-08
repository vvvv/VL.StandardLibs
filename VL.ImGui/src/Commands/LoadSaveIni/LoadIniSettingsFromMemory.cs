namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class LoadIniSettingsFromMemory : ChannelWidget<string>
    {

        public bool Enabled { private get; set; } = true;

        internal override void UpdateCore(Context context)
        {
            if (Enabled)
            {
                var value = Update();
                if (value != null)
                    ImGuiNET.ImGui.LoadIniSettingsFromMemory(value);
            }
        }
    }
}
