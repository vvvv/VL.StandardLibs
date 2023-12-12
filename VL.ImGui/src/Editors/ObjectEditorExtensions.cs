using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.ImGui.Editors
{
    using ImGui = ImGuiNET.ImGui;

    public static class ObjectEditorExtensions
    {
        public static IObjectEditor ToViewOnly(this IObjectEditor objectEditor, ObjectEditorContext ctx)
        {
            if (ctx.ViewOnly) 
                return new ViewOnlyEditor(objectEditor);
            return objectEditor;
        }

        private sealed class ViewOnlyEditor : IObjectEditor
        {
            private readonly IObjectEditor _editor;

            public ViewOnlyEditor(IObjectEditor editor)
            {
                _editor = editor;
            }

            public void Draw(Context? context)
            {
                ImGui.BeginDisabled();
                try
                {
                    _editor.Draw(context);
                }
                finally
                {
                    ImGui.EndDisabled();
                }
            }
        }
    }
}
