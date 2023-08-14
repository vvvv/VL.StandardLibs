namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Create a Menu. You can call Menu multiple time with the same Label to append more items to it.
    /// </summary>
    [GenerateNode(Category = "ImGui.Widgets", GenerateImmediate = false)]
    internal sealed partial class Menu : Widget
    {

        public Widget? Content { private get; set; }

        public string? Label { get; set; }

        public bool Enabled { get; set; } = true;

        internal override void UpdateCore(Context context)
        {

            if (ImGuiNET.ImGui.BeginMenu(widgetLabel.Update(Label), Enabled))
            {
                try
                {
                    context.Update(Content);
                }
                finally
                {
                    ImGuiNET.ImGui.EndMenu();
                }
                    
            }
        }
    }
}
