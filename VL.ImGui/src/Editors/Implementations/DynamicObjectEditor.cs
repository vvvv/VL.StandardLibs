#nullable enable
using VL.Core;
using VL.ImGui.Editors.Implementations;
using VL.Lib.Reactive;

namespace VL.ImGui.Editors;

using ImGui = ImGuiNET.ImGui;

/// <summary>
/// Switches between different object editors based on the runtime type of the object in the channel
/// </summary>
sealed class DynamicObjectEditor : IObjectEditor, IDisposable
{
    readonly IChannel channel;
    readonly ObjectEditorContext editorContext;

    Type? currentType;
    IObjectEditor? currentEditor;

    public DynamicObjectEditor(IChannel channel, ObjectEditorContext editorContext)
    {
        this.channel = channel;
        this.editorContext = editorContext;
        RecreateEditor(channel.Object?.GetType());
    }

    public void Dispose()
    {
        DisposeEditor();
    }

    private void DisposeEditor()
    {
        if (currentEditor is IDisposable disposable)
            disposable.Dispose();
        currentEditor = null;
    }

    public bool NeedsMoreThanOneLine => Query(e => e.NeedsMoreThanOneLine);

    public bool HasContentToDraw => Query(e => e.HasContentToDraw);

    public void Draw(Context? context)
    {
        context = context.Validate();
        if (context is null)
            return;

        var value = channel.Object;
        var type = value?.GetType();
        if (type != currentType)
        {
            RecreateEditor(type);
        }

        if (currentEditor != null)
        {
            currentEditor.Draw(context);
        }
        else
        {
            var s = value?.ToString() ?? "NULL";
            if (!string.IsNullOrEmpty(editorContext.Label))
                ImGui.LabelText(editorContext.LabelForImGUI, s);
            else
                ImGui.TextUnformatted(s);
        }
    }

    private T? Query<T>(Func<IObjectEditor, T> func)
    {
        var value = channel.Object;
        var type = value?.GetType();
        if (type != currentType)
            RecreateEditor(type);

        if (currentEditor != null)
            return func(currentEditor);

        return default;
    }

    private void RecreateEditor(Type? type)
    {
        currentType = type;

        DisposeEditor();

        if (type != null && type != typeof(object))
        {
            if (type.TryGetMonadicType(out var monadicType))
            {
                var channelView = channel.AsChannelView(monadicType);
                currentEditor = MonadicEditor.Create(channelView, editorContext);
            }
            else
            {
                // Editors get registered for typed channels - let's create a typed view ask the factory to create an editor for it
                var channelView = channel.AsChannelView(type);
                currentEditor = editorContext.Factory.CreateObjectEditor(channelView, editorContext);
            }
        }
    }
}
