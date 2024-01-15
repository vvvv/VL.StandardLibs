using Stride.Core.Extensions;
using VL.Lib.Reactive;

namespace VL.ImGui.Editors
{
    using ImGui = ImGuiNET.ImGui;

    sealed class EnumEditor<T> : IObjectEditor
    {
        readonly IChannel<T> channel;
        readonly string label;
        readonly T[] values;
        readonly string[] names;

        public EnumEditor(IChannel<T> channel, ObjectEditorContext editorContext)
        {
            this.channel = channel;
            this.label = editorContext.LabelForImGUI;
            this.values = (T[])Enum.GetValues(typeof(T));
            this.names = Enum.GetNames(typeof(T));
        }

        public void Draw(Context? context)
        {
            var @enum = channel.Value!;
            var currentItem = values.IndexOf(@enum);
            if (ImGui.Combo(label, ref currentItem, names, names.Length))
                channel.Value = values[currentItem];
        }
    }
}
