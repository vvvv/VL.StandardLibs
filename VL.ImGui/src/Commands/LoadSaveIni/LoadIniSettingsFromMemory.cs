namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class LoadIniSettingsFromMemory : ChannelWidget<string>
    {

        public bool Enabled { private get; set; } = true;

        internal override void UpdateCore(Context context)
        {
            context.AddToResetQueue(this);
        }

        internal override void Reset()
        {
            // LoadIniSettings must be called outside of BeginFrame, https://github.com/ocornut/imgui/issues/2573#issuecomment-625512092
            if (Enabled)
            {
                var value = Update();
                if (value != null)
                    ImGuiNET.ImGui.LoadIniSettingsFromMemory(value);
            }

            base.Reset();
        }
    }
}
