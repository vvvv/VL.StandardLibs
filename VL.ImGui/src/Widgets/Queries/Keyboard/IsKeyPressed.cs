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
            // Watch out for default, causes access violation in native code
            var key = Key.ToImGuiKey();
            if (key != default)
                Value = ImGuiNET.ImGui.IsKeyPressed(key, Repeat);
            else
                Value = false;
        }
    }
}
