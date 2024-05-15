using VL.Lib.IO;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Is key pressed?
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries")]
    internal partial class IsKeyPressed : Query
    {

        public Keys Key { private get; set; }

        public bool Repeat { private get; set; }

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsKeyPressed(Key.ToImGuiKey(), Repeat);
        }
    }
}
