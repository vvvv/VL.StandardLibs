﻿using ImGuiNET;
using VL.Core.EditorAttributes;
using NativeImGuiInputTextFlags = ImGuiNET.ImGuiInputTextFlags;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Input (String Callback)", Category = "ImGui.Widgets", Tags = "edit, textfield")]
    [WidgetType(WidgetType.Input)]
    internal unsafe partial class InputTextWithCallback : ChannelWidget<string>, IHasInputTextFlags
    {
        public int MaxLength { get; set; } = 100;

        public ImGuiInputTextFlags Flags { get; set; }

        public CreateHandler? Create { get; set; }
        public CharFilterHandler? Filter { get; set; }
        public TextCallbackHandler? Edit { get; set; }
        public TextCallbackHandler? Always { get; set; }
        public TextCallbackHandler? Completion { get; set; }
        public TextCallbackHandler? History { get; set; }

        string? lastframeValue;
        object? state;

        internal override void UpdateCore(Context context)
        {
            var value = base.Update() ?? string.Empty;
            if (ImGuiUtils.InputText(widgetLabel.Update(label.Value), ref value, (uint)MaxLength, Flags, TextCallback))
                SetValueIfChanged(lastframeValue, value, Flags);
            lastframeValue = value;
        }

        protected override void Dispose(bool disposing)
        {
            if (state is IDisposable disposable)
                disposable.Dispose();

            base.Dispose(disposing);
        }

        private int TextCallback(ImGuiInputTextCallbackData* data)
        {
            if (Create is null)
                return 0;

            if (state is null)
                Create.Invoke(out state);

            switch (data->EventFlag)
            {
                case NativeImGuiInputTextFlags.CallbackCompletion:
                    Completion?.Invoke(state, new InputTextCallbackData(data), out state);
                    break;
                case NativeImGuiInputTextFlags.CallbackHistory:
                    History?.Invoke(state, new InputTextCallbackData(data), out state);
                    break;
                case NativeImGuiInputTextFlags.CallbackCharFilter:
                    if (Filter != null)
                    {
                        var allowCharacter = false;
                        Filter(state, new InputTextCallbackData(data), out state, out allowCharacter);
                        return allowCharacter ? 0 : 1;
                    }
                    break;
                case NativeImGuiInputTextFlags.CallbackAlways:
                    Always?.Invoke(state, new InputTextCallbackData(data), out state);
                    break;
                case NativeImGuiInputTextFlags.CallbackResize:
                    break;
                case NativeImGuiInputTextFlags.CallbackEdit:
                    Edit?.Invoke(state, new InputTextCallbackData(data), out state);
                    break;
            }
            return 0;
        }
    }
}
