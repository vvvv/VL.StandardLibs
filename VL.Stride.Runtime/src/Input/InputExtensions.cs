using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Rendering;
using System.Runtime.CompilerServices;

namespace VL.Stride.Input
{
    public static class InputExtensions
    {
        /// <summary>
        /// A property key to get the window input source from the <see cref="ComponentBase.Tags"/>.
        /// </summary>
        public static readonly PropertyKey<IInputSource> WindowInputSource = new PropertyKey<IInputSource>("WindowInputSource", typeof(IInputSource));

        public static RenderContext SetWindowInputSource(this RenderContext input, IInputSource inputSource)
        {
            input.Tags.Set(WindowInputSource, inputSource);
            return input;
        }

        public static IInputSource GetWindowInputSource(this RenderContext input) => input.Tags.Get(WindowInputSource);

        public static IInputSource GetDevices(this IInputSource inputSource, out IMouseDevice mouseDevice, out IKeyboardDevice keyboardDevice, out IPointerDevice pointerDevice)
        {
            mouseDevice = null;
            keyboardDevice = null;
            pointerDevice = null;

            if (inputSource != null)
            {
                foreach (var item in inputSource.Devices)
                {
                    var device = item.Value;

                    if (device is IMouseDevice mouse)
                        mouseDevice = mouse;

                    else if (device is IKeyboardDevice keyboard)
                        keyboardDevice = keyboard;

                    else if (device is IPointerDevice pointer)
                        pointerDevice = pointer;
                }
            }

            return inputSource;
        }

        public static void UpdateSurfaceArea(this IInputSource inputSource, Vector2 size)
        {
            if (inputSource != null)
            {
                foreach (var item in inputSource.Devices)
                {
                    var device = item.Value;
                    if (device is PointerDeviceBase pointer)
                        pointer.SetSurfaceSize(size);
                }
            }
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(SetSurfaceSize))]
        extern static void SetSurfaceSize(this PointerDeviceBase device, Vector2 newSize);

        /// <summary>
        /// The priority of the input devices. Larger means higher priority when selecting the first device of some type.
        /// </summary>
        public static void SetPriority(this IInputSource inputSource, int priority)
        {
            foreach (var (_, device) in inputSource.Devices)
                device.Priority = priority;
        }

        public static void AddWithPriority(this InputManager inputManager, IInputSource inputSource, int priority)
        {
            // First add the input source to the input manager - this will initialize the devices
            inputManager.Sources.Add(inputSource);
            // Now set the priority of each device - sadly this does not update the input manager
            inputSource.SetPriority(priority);
            // Therefor trigger the internal refresh by adding and removing a simulated input source
            var sim = new InputSourceSimulated();
            inputManager.Sources.Add(sim);
            // Devices must be added after the input source has been added because only now the manager is listening
            sim.AddGamePad();
            sim.AddKeyboard();
            sim.AddMouse();
            sim.AddPointer();
            inputManager.Sources.Remove(sim);
        }
    }
}
