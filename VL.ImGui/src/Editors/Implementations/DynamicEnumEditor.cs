using Stride.Core.Extensions;
using VL.Lib.Collections;
using VL.Lib.Reactive;

namespace VL.ImGui.Editors
{
    using ImGui = ImGuiNET.ImGui;

    sealed class DynamicEnumEditor<T> : IObjectEditor
        where T: IDynamicEnum
    {
        readonly IChannel<T> channel;
        readonly string label;
        string[] names;

        public DynamicEnumEditor(IChannel<T> channel, ObjectEditorContext editorContext)
        {
            this.channel = channel;
            this.label = editorContext.LabelForImGUI;
        }

        IDynamicEnum dynamicEnum;
        IDynamicEnum DynamicEnum
        {
            get => dynamicEnum;
            set
            {
                if (dynamicEnum != value)
                {
                    dynamicEnum = value;
                    if (value != null)
                        names = dynamicEnum.Definition.Entries.ToArray();
                }
            }
        }

        public void Draw(Context? context)
        {
            DynamicEnum = channel.Value!;
            if (DynamicEnum != null)
            {
                var currentItem = names.IndexOf(DynamicEnum.Value);
                if (ImGui.Combo(label, ref currentItem, names, names.Length))
                    channel.Value = (T)DynamicEnum.CreateValue(names[currentItem]);
            }
        }
    }
}
