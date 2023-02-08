using VL.Lib.Reactive;

namespace VL.ImGui.Editors
{
    sealed class ArrayEditor<T> : ListEditorBase<T[], T>
    {
        public ArrayEditor(Channel<T[]> channel, ObjectEditorContext editorContext)
            : base(channel, editorContext)
        {
        }

        protected override T[] SetItem(T[] list, int i, T item)
        {
            list[i] = item;
            return list;
        }
    }
}
