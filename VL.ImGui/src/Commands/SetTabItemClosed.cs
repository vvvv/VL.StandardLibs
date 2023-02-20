namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Notify TabBar or Docking system of a closed tab/window ahead (useful to reduce visual flicker on reorderable tab bars). 
    /// For TabBar: call inside TabBar region before Tab submissions. Otherwise call with a window name.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class SetTabItemClosed : Widget
    {
        public string? Label { private get; set; }
        public bool Enabled { private get; set; } = true;

        internal override void UpdateCore(Context context)
        {
            if (Label != null && Enabled)
               ImGuiNET.ImGui.SetTabItemClosed(Label);
        }
    }
}
