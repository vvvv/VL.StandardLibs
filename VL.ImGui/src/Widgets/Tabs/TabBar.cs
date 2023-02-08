namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets", GenerateImmediate = false)]
    internal sealed partial class TabBar : Widget
    {
        public Widget? Content { get; set; }

        public string? Label { get; set; }

        public ImGuiNET.ImGuiTabBarFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            if (ImGuiNET.ImGui.BeginTabBar(Context.GetLabel(this, Label), Flags))
            {
                try
                {
                    context.Update(Content);
                }
                finally
                {
                    ImGuiNET.ImGui.EndTabBar();
                }
            }
        }
    }
}
