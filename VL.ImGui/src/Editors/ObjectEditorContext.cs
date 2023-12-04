using MathNet.Numerics;
using System.Reflection.Emit;
using VL.Core;

namespace VL.ImGui.Editors
{
    public class ObjectEditorContext
    {
        public readonly AppHost AppHost;
        public readonly IObjectEditorFactory Factory;
        public readonly string? Label;
        public readonly bool ViewOnly;
        public readonly bool PrimitiveOnly;
        public readonly bool IsSubContext;
        public readonly string LabelForImGUI;
    
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

        public ObjectEditorContext CreateSubContext(string label = default)
        {
            return new ObjectEditorContext(AppHost, Factory, label, viewOnly: ViewOnly, primitiveOnly: PrimitiveOnly);
        }
    }
}
