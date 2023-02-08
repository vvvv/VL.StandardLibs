namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Queries", GenerateRetained = false)]
    internal partial class IsWindowAppearing : Widget
    {

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsWindowAppearing();
        }
    }
}