namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Adjust scrolling amount to make current cursor position visible.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class SetScrollFromPosX : Widget
    {
        public float Value { private get; set; }

        /// <summary>
        /// 0.0 - Left, 0.5 - Center, 1.0 - Right.
        /// </summary>
        public float Ratio { private get; set; }

        public bool Enabled { private get; set; } = true;

        internal override void UpdateCore(Context context)
        {
            if (Enabled)
                ImGuiNET.ImGui.SetScrollFromPosX(Value.FromHectoToImGui(), Ratio);
        }
    }
}
