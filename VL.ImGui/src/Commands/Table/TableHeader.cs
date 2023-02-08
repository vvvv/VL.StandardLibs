namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    /// <summary>
    /// Submit one header cell manually (rarely used)
    /// </summary>
    internal partial class TableHeader : Widget
    {

        public string? Label { private get; set; }

        internal override void UpdateCore(Context context)
        {
            ImGuiNET.ImGui.TableHeader(Context.GetLabel(this, Label));
        }
    }
}