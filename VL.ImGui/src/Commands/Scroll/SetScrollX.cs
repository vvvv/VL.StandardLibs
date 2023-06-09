namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Set scrolling amount
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class SetScrollX : Widget
    {
        public float Value { private get; set; }

        public bool Enabled { private get; set; } = true;

        protected override void UpdateCore(Context context)
        {
            if (Enabled)
                ImGuiNET.ImGui.SetScrollX(Value.FromHectoToImGui());
        }
    }
}
