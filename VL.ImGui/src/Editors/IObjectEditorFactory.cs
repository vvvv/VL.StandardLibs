using VL.Lib.Reactive;

namespace VL.ImGui.Editors
{
    public interface IObjectEditorFactory
    {
        public IObjectEditor? CreateObjectEditor(Channel channel, ObjectEditorContext context);
    }
}
