using VL.Lib.Reactive;

namespace VL.ImGui.Editors
{
    public interface IObjectEditorFactory
    {
        public IObjectEditor? CreateObjectEditor(IChannel channel, ObjectEditorContext context);
    }
}
