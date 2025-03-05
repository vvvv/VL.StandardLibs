using ImGuiNET;
using System.Text;
using VL.Lib.IO;

namespace VL.ImGui.Widgets
{
    public delegate void CreateHandler(out object stateOutput);
    public delegate void TextCallbackHandler(object stateInput, InputTextCallbackData data, out object stateOutput);
    public delegate void CharFilterHandler(object stateInput, InputTextCallbackData data, out object stateOutput, out bool allowCharacter);

    public unsafe struct InputTextCallbackData
    {
        private readonly ImGuiInputTextCallbackData* data;

        internal InputTextCallbackData(ImGuiInputTextCallbackData* data)
        {
            this.data = data;
        }

        /// <summary>
        /// Character input.
        /// </summary>
        /// <remarks>
        /// [CharFilter] Replace character with another one, or set to zero to drop. return 1 is equivalent to setting EventChar=0;
        /// </remarks>
        public char EventChar { get => (char)data->EventChar; set => data->EventChar = value; }

        /// <summary>
        /// Key pressed (Up/Down/TAB)
        /// </summary>
        /// <remarks>
        /// [Completion,History]
        /// </remarks>
        public Keys EventKey => data->EventKey.ToKeys();

        /// <summary>
        /// Text buffer
        /// </summary>
        /// <remarks>
        /// [Resize] Can replace pointer / [Completion,History,Always]
        /// </remarks>
        public string Text
        {
            get
            {
                var span = new Span<byte>(data->Buf, data->BufTextLen);
                return Encoding.UTF8.GetString(span);
            }
            set
            {
                var span = new Span<byte>(data->Buf, data->BufSize);
                Encoding.UTF8.TryGetBytes(value, span.Slice(0, span.Length - 1 /* Exclude zero-terminator */), out data->BufTextLen);
                span[data->BufTextLen] = 0; // Zero-terminate
                data->BufDirty = 1;
            }
        }

        /// <remarks>
        /// [Completion,History,Always]
        /// </remarks>
        public int CursorPos
        {
            get => data->CursorPos;
            set => data->CursorPos = value;
        }

        /// <remarks>
        /// [Completion,History,Always] == to SelectionEnd when no selection)
        /// </remarks>
        public int SelectionStart
        {
            get => data->SelectionStart;
            set => data->SelectionStart = value;
        }

        /// <remarks>
        /// [Completion,History,Always]
        /// </remarks>
        public int SelectionEnd
        {
            get => data->SelectionEnd;
            set => data->SelectionEnd = value;
        }
    }
}
