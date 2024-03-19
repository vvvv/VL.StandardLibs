using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Input;
using Stride.Graphics;
using System.Numerics;
using VL.Stride.Input;
using Vector2 = Stride.Core.Mathematics.Vector2;

namespace VL.ImGui
{
    public class ImputSourceViewPort : InputSourceBase , 
        IInputEventListener<PointerEvent>, IInputEventListener<MouseButtonEvent>, IInputEventListener<MouseWheelEvent>
    {
        private readonly InputManager inputManager;
        private readonly MouseViewPort mouseViewPort;
        private readonly PointerViewPort pointerViewPort;
        private readonly IWithParentInputSourceAndViewport withInputSourceAndViewport;

        internal ImputSourceViewPort(IWithParentInputSourceAndViewport withInputSourceAndViewport, InputManager inputManager)
        {
            mouseViewPort = new MouseViewPort(this);
            pointerViewPort = new PointerViewPort(this);

            this.inputManager = inputManager;
            this.withInputSourceAndViewport = withInputSourceAndViewport;

            this.inputManager.Sources.Add(this);   
        }

        public override void Initialize(InputManager inputManager)
        {
            this.inputManager.AddListener(this);
            RegisterDevice(mouseViewPort);
            RegisterDevice(pointerViewPort);
        }

        public void ProcessEvent(PointerEvent inputEvent)
        {
            if (inputEvent.Device.Source == withInputSourceAndViewport.ParentInputSource) 
            {
                mouseViewPort.SetPosition(inputEvent.Position);
                mouseViewPort.UpdateSurfaceArea(inputEvent.Pointer.SurfaceSize);
            }
        }
        public void ProcessEvent(MouseButtonEvent inputEvent)
        {
            if (inputEvent.Device.Source == withInputSourceAndViewport.ParentInputSource)
            {
                if (inputEvent.IsDown)
                    mouseViewPort.SimulateMouseDown(inputEvent.Button);
                else
                    mouseViewPort.SimulateMouseUp(inputEvent.Button);
            }
        }

        public void ProcessEvent(MouseWheelEvent inputEvent)
        {
            if (inputEvent.Device.Source == withInputSourceAndViewport.ParentInputSource)
            {
                mouseViewPort.SimulateMouseWheel(inputEvent.WheelDelta);
            }
        }

        public override void Dispose()
        {
            UnregisterDevice(mouseViewPort);
            UnregisterDevice(pointerViewPort);
            this.inputManager.Sources.Remove(this);
            this.inputManager.RemoveListener(this);
            base.Dispose();
        }

        
    }
}
