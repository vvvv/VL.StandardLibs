namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class LoadIniSettingsFromDisk : Widget
    {

        public string? Filename { private get; set; }

        public bool Enabled { private get; set; } = false;

        internal override void UpdateCore(Context context)
        {
            if (Enabled && Filename != null)
                ImGuiNET.ImGui.LoadIniSettingsFromDisk(Filename);
        }
    }
}
