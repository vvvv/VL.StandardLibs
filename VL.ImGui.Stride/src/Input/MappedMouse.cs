namespace Stride.Input
{
    using Stride.Core.Collections;
    using Stride.Core.Mathematics;

    public class MappedMouse : MappedPointerBase, IMouseDevice, IMappedDevice
    {
        private  IMouseDevice mouse;

        public MappedMouse(IMouseDevice mouse, MappedInputSource source) : base(mouse, source) 
        {
            this.mouse = mouse;
        }

        public new bool SetSourceDevice(IInputDevice device)
        {
            if (device is IMouseDevice mouse)
            {
                this.mouse = mouse;
                return true;
            }
            else
            {
                return false;
            }
        }

        public Vector2 Position => mouse.Position.transformPos(mouse, source);
        
        public Vector2 Delta => mouse.Delta.transformDelta(mouse, source);

        public IReadOnlySet<MouseButton> PressedButtons => mouse.PressedButtons;

        public IReadOnlySet<MouseButton> ReleasedButtons => mouse.ReleasedButtons;

        public IReadOnlySet<MouseButton> DownButtons => mouse.DownButtons;

        public bool IsPositionLocked => mouse.IsPositionLocked;

        public override string Name => "Mapped Mouse";

        int IInputDevice.Priority { get; set; }

        public void LockPosition(bool forceCenter = false)
        {
            mouse.LockPosition(forceCenter);
        }

        public void SetPosition(Vector2 normalizedPosition)
        {
            mouse.SetPosition(normalizedPosition);
        }

        public void UnlockPosition()
        {
            mouse.UnlockPosition();
        }
    }
}
