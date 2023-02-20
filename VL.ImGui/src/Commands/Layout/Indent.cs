namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Move content position toward the right, by Value, or style.IndentSpacing if Value &lt;= 0.
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class Indent : Widget
    {

        public float Value { private get; set; } = 0.5f;

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.Indent(Value.FromHectoToImGui());
        }
    }
}
