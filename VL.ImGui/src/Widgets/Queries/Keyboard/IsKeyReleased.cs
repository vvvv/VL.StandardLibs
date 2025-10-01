using VL.Lib.IO;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Is key released?
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries")]
    internal partial class IsKeyReleased : Query
    {

        public Keys Key { private get; set; }

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsKeyReleased(Key.ToImGuiKey());
        }
    }
}
