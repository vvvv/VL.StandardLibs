using VL.Core;

namespace VL.ImGui.Editors
{
    public record ObjectEditorContext(
        AppHost AppHost, 
        IObjectEditorFactory Factory, 
        string? Label = null, 
        bool ViewOnly = false, 
        bool PrimitiveOnly = false);
}
