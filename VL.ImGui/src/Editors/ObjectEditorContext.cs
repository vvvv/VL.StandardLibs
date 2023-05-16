using VL.Core;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Editors
{
    public record ObjectEditorContext(AppHost AppHost, IObjectEditorFactory Factory, string? Label = null, bool ViewOnly = false);
}
