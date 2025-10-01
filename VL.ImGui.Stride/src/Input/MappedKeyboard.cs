using Stride.Core.Mathematics;

namespace Stride.Input
{
    using Stride.Core.Collections;
    public class MappedKeyboard : IKeyboardDevice, IMappedDevice
    {
        private IKeyboardDevice keyboard;
        private readonly MappedInputSource source;

        public MappedKeyboard(IKeyboardDevice keyboard, MappedInputSource source)
        {
            this.keyboard = keyboard;
            this.source = source;
        }

        public bool SetSourceDevice(IInputDevice device)
        {
            if (device is IKeyboardDevice keyboard)
            {
                this.keyboard = keyboard;
                return true;
            }
            else
            {
                return false;
            }
        }

        public Guid SourceDeviceId => keyboard.Id;

        public IReadOnlySet<Keys> PressedKeys => keyboard.PressedKeys;

        public IReadOnlySet<Keys> ReleasedKeys => keyboard.ReleasedKeys;

        public IReadOnlySet<Keys> DownKeys => keyboard.DownKeys;

        public string Name => "Mapped Keyboard";

        public Guid Id => Guid.NewGuid();

        public int Priority { get; set; }

        public IInputSource Source => source;

        public void Update(List<InputEvent> inputEvents)
        {
        }
    }
}
