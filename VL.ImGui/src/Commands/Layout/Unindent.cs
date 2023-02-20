namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Move content position back to the Left, by Value, or style.IndentSpacing if Value &lt;= 0.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class Unindent : Widget
    {

        public float Value { private get; set; } = 0.5f;

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.Unindent(Value.FromHectoToImGui());
        }
    }
}
