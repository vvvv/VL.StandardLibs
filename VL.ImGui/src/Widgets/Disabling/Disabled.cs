namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets", GenerateImmediate = false)]
    internal sealed partial class Disabled : Widget
    {

        public Widget? Input { private get; set; }

        public bool Apply { private get; set; } = true;

        internal override void UpdateCore(Context context)
        {

            if (Apply)
            {
                ImGuiNET.ImGui.BeginDisabled();

                try
                {
                    context.Update(Input);
                }
                finally
                {
                    ImGuiNET.ImGui.EndDisabled();
                }
            }
            else
            {
                context.Update(Input);
            }    

        }
    }
}
