using VL.Lib.Collections;
using VL.Lib.Reactive;

namespace VL.ImGui.Editors
{
    sealed class SpreadEditor<T> : ListEditorBase<Spread<T>, T>
    {
        public SpreadEditor(Channel<Spread<T>> channel, ObjectEditorContext editorContext)
            : base(channel, editorContext)
        {
        }

        protected override Spread<T> SetItem(Spread<T> list, int i, T item)
        {
            return list.SetItem(i, item);
        }
    }
}
