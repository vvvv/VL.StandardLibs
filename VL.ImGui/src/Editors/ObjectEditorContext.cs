using VL.Core.EditorAttributes;

namespace VL.ImGui.Editors
{
    public record ObjectEditorContext(IObjectEditorFactory Factory, string? Label = null, bool ViewOnly = false);
}
