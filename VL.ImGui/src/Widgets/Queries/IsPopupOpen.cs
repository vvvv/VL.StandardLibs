namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Return true if the popup is open at the current BeginPopup() level of the popup stack. With AnyPopupId flag: return true if any popup is open at the current BeginPopup() level of the popup stack. With AnyPopupId + AnyPopupLevel flags: return true if any popup is open.
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", GenerateRetained = false, IsStylable = false)]
    internal partial class IsPopupOpen : Query
    {
        public string? Label { private get; set; }

        public ImGuiNET.ImGuiPopupFlags Flags { private get; set; }

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsPopupOpen(widgetLabel.Update(Label), Flags);
        }
    }
}
