namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Did the last item modify its underlying value this frame? or was pressed? This is generally the same as the 'bool' return value of many widgets.
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class IsItemEdited : Widget
    {

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsItemEdited();
        }
    }
}
