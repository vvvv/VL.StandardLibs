using MathNet.Numerics.Providers.LinearAlgebra;
using Stride.Input;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace VL.Stride.Input;

// Adds text input support (native KeyboardWinforms does't work, even when calling EnabledTextInput)
internal class TextKeyboardWinforms : KeyboardDeviceBase, ITextInputDevice, IDisposable
{
    private readonly Form form;
    private readonly List<TextInputEvent> textEvents = new List<TextInputEvent>();

    public TextKeyboardWinforms(IInputSource inputSource, Form form)
    {
        Source = inputSource;

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

    public override string Name => "Windows Keyboard Text";

    public override Guid Id { get; }

    public override IInputSource Source { get; }

    public override void Update(List<InputEvent> inputEvents)
    {
        base.Update(inputEvents);

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
