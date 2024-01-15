using ImGuiNET;
using VL.ImGui.Widgets;
using VL.Lib.Reactive;

namespace VL.ImGui.Editors
{
    sealed class ObjectEditorBasedOnChannelWidget<T> : IObjectEditor
    {
        public ChannelWidget<T> Widget { get; }

        public ObjectEditorBasedOnChannelWidget(IChannel<T> channel, ObjectEditorContext context, Type widgetClass)
        {
            var widget = (ChannelWidget<T>)Activator.CreateInstance(widgetClass)!;
            Widget = widget;

            widget.Channel = channel;

            if (!string.IsNullOrEmpty(context.Label) && widget is IHasLabel hasLabel)
                hasLabel.Label = context.Label;

            if (widget is IHasInputTextFlags hasInputTextFlags)
                hasInputTextFlags.Flags = ImGuiInputTextFlags.EnterReturnsTrue;
        }

        public void Draw(Context? context)
        {
            Widget?.Update(context);
        }
    }
}
