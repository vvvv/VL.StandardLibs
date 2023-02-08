using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets")]
    internal partial class ListBox : ChannelWidget<string>
    {

        public string? Label { get; set; }

        public Vector2 Size { get; set; } = Vector2.Zero;

        public IEnumerable<string> Items { get; set; } = Enumerable.Empty<string>();

        internal override void UpdateCore(Context context)
        {
            var value = Update();

            var count = Items.Count();
            if (count > 0)
            {
                if (ImGuiNET.ImGui.BeginListBox(Context.GetLabel(this, Label), Size.FromHectoToImGui()))
                {
                    try
                    {
                        foreach (var item in Items)
                        {
                            bool is_selected = value == item;
                            if (ImGuiNET.ImGui.Selectable(item, is_selected))
                            {
                                Value = item;
                            }
                            if (is_selected)
                            {
                                ImGuiNET.ImGui.SetItemDefaultFocus();
                            }
                        }
                    }
                    finally
                    {
                        ImGuiNET.ImGui.EndListBox();
                    }
                }
            }
        }
    }
}
