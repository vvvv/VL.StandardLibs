using Stride.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Keys = Stride.Input.Keys;

namespace VL.Stride.Input;

// Adds text input support (native KeyboardWinforms does't work, even when calling EnabledTextInput)
// Not needed if original keyboard would emit those events correctly
internal class TextKeyboardWinforms : IKeyboardDevice, ITextInputDevice, IDisposable
{
    private readonly Form form;
    private readonly IKeyboardDevice keyboard;
    private readonly List<TextInputEvent> textEvents = new List<TextInputEvent>();

    public TextKeyboardWinforms(IInputSource inputSource, Form form)
    {
        Source = inputSource;
        keyboard = inputSource.Devices.Values.OfType<IKeyboardDevice>().First();

        this.form = form;
        form.KeyPress += Form_KeyPress;

        Id = InputDeviceUtils.DeviceNameToGuid(form.Handle.ToString() + Name);
    }

    private void Form_KeyPress(object sender, KeyPressEventArgs e)
    {
        var inputEvent = InputEventPool<TextInputEvent>.GetOrCreate(this);
        inputEvent.Text = e.KeyChar.ToString();
        inputEvent.Type = TextInputEventType.Input;
        textEvents.Add(inputEvent);
    }

    public string Name => "Windows Keyboard Text";

    public Guid Id { get; }

    public IInputSource Source { get; }

    public global::Stride.Core.Collections.IReadOnlySet<Keys> PressedKeys => keyboard.PressedKeys;

    public global::Stride.Core.Collections.IReadOnlySet<Keys> ReleasedKeys => keyboard.ReleasedKeys;

    public global::Stride.Core.Collections.IReadOnlySet<Keys> DownKeys => keyboard.DownKeys;

    public int Priority { get => keyboard.Priority; set => keyboard.Priority = value; }

    public void Update(List<InputEvent> inputEvents)
    {
        for (int i = 0; i < textEvents.Count; i++)
            inputEvents.Add(textEvents[i]);
        textEvents.Clear();
    }

    public void DisableTextInput()
    {
    }

    public void EnabledTextInput()
    {
    }

    public void Dispose()
    {
        form.KeyPress -= Form_KeyPress;
    }
}
