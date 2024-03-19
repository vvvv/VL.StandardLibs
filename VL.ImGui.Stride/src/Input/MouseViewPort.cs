using Stride.Core.Mathematics;
using Stride.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.ImGui
{
    public class MouseViewPort : MouseDeviceBase
    {
        private bool positionLocked;
        private Vector2 capturedPosition;

        public MouseViewPort(ImputSourceViewPort source)
        {
            Source = source;
            Id = Guid.NewGuid();
        }

        public override bool IsPositionLocked => positionLocked;

        public override string Name => "ViewPort Mouse";

        public override Guid Id { get; }

        public override IInputSource Source { get; }

        public override void LockPosition(bool forceCenter = false)
        {
            positionLocked = true;
            capturedPosition = forceCenter ? new Vector2(0.5f) : Position;
        }

        public override void SetPosition(Vector2 normalizedPosition)
        {
            if (IsPositionLocked)
            {
                MouseState.HandleMouseDelta(normalizedPosition * SurfaceSize - capturedPosition);
            }
            else
            {
                MouseState.HandleMove(normalizedPosition * SurfaceSize);
            }
        }

        public void SimulateMouseDown(MouseButton button)
        {
            MouseState.HandleButtonDown(button);
        }

        public void SimulateMouseUp(MouseButton button)
        {
            MouseState.HandleButtonUp(button);
        }

        public void SimulateMouseWheel(float wheelDelta)
        {
            MouseState.HandleMouseWheel(wheelDelta);
        }

        public override void UnlockPosition()
        {
            positionLocked = false;
        }
    }
}
