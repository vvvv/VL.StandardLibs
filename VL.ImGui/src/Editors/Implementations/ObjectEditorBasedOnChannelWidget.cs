using VL.ImGui.Widgets;
using VL.Lib.Reactive;

namespace VL.ImGui.Editors
{
    sealed class ObjectEditorBasedOnChannelWidget<T> : IObjectEditor
    {
        public ChannelWidget<T> Widget { get; }

        public ObjectEditorBasedOnChannelWidget(Channel<T> channel, ObjectEditorContext context, Type widgetClass)
        {
            var widget = (ChannelWidget<T>)Activator.CreateInstance(widgetClass)!;
            Widget = widget;

            widget.Channel = channel;
            if (!string.IsNullOrEmpty(context.Label))
            {
                var labelProperty = widgetClass.GetProperty(nameof(InputFloat.Label));
                if (labelProperty != null && labelProperty.PropertyType == typeof(string))
                    labelProperty.SetValue(widget, context.Label);
            }
        }

        public void Draw(Context? context)
        {
            Widget?.Update(context);
        }
    }
}
