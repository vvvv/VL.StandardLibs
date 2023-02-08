using VL.Lib.Reactive;

namespace VL.ImGui.Editors
{
    using ImGui = ImGuiNET.ImGui;

    sealed class EnumEditor<T> : IObjectEditor
    {
        readonly Channel<T> channel;
        readonly string label;

        public EnumEditor(Channel<T> channel, ObjectEditorContext editorContext)
        {
            this.channel = channel;
            this.label = editorContext.Label ?? $"##{GetHashCode()}";
        }

        public void Draw(Context? context)
        {
            var @enum = channel.Value!;
            var currentItem = (int)Convert.ChangeType(@enum, typeof(int));
            var names = Enum.GetNames(@enum.GetType());
            if (ImGui.Combo(label, ref currentItem, names, names.Length))
                channel.Value = (T)Enum.ToObject(@enum.GetType(), currentItem);
        }
    }
}
