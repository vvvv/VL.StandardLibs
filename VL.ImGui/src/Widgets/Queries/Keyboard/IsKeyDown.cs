using VL.Lib.IO;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Is key down?
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries")]
    internal partial class IsKeyDown : Query
    {

        public Keys Key { private get; set; }

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsKeyDown(Key.ToImGuiKey());
        }
    }
}
