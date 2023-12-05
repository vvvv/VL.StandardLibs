using VL.Core;

namespace VL.ImGui.Editors
{
    public class ObjectEditorContext
    {
        public AppHost AppHost { get; }
        public IObjectEditorFactory Factory { get; }
        public string? Label { get; }
        public bool ViewOnly { get; }
        public bool PrimitiveOnly { get; }
        public bool IsSubContext { get; }
        public string LabelForImGUI { get; }

        public ObjectEditorContext(
            AppHost appHost,
            IObjectEditorFactory factory,
            string? label = null,
            bool viewOnly = false,
            bool primitiveOnly = false,
            bool isSubContext = false)
        {
            AppHost = appHost;
            Factory = factory;
            Label = label;
            ViewOnly = viewOnly;
            PrimitiveOnly = primitiveOnly;
            IsSubContext = isSubContext;

            // benefit from once defined label logic
            LabelForImGUI = new WidgetLabel().Update(label);
        }

        public ObjectEditorContext CreateSubContext(string? label = default)
        {
            return new ObjectEditorContext(AppHost, Factory, label, viewOnly: ViewOnly, primitiveOnly: PrimitiveOnly);
        }
    }
}
