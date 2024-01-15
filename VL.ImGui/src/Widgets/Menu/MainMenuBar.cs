namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Create a MenuBar at the top of the screen.
    /// </summary>
    [GenerateNode(Category = "ImGui.Widgets", GenerateImmediate = false)]
    internal partial class MainMenuBar : Widget
    {
        public Widget? Content { private get; set; }

        internal override void UpdateCore(Context context)
        {
            if (ImGuiNET.ImGui.BeginMainMenuBar())
            {
                try
                {
                    context.Update(Content);
                }
                finally
                {
                    ImGuiNET.ImGui.EndMenuBar();
                }
            }
        }
    }
}
