namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class LoadIniSettingsFromDisk : Widget
    {

        public string? Filename { private get; set; }

        public bool Enabled { private get; set; } = false;

        internal override void UpdateCore(Context context)
        {
            context.AddToResetQueue(this);
        }

        internal override void Reset()
        {
            // LoadIniSettings must be called outside of BeginFrame, https://github.com/ocornut/imgui/issues/2573#issuecomment-625512092
            if (Enabled && Filename != null)
                ImGuiNET.ImGui.LoadIniSettingsFromDisk(Filename);

            base.Reset();
        }
    }
}
