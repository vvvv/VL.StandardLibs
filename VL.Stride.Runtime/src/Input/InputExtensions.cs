using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Rendering;

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
                    if (device is IPointerDevice pointer)
                        pointer.UpdateSurfaceArea(size);
                }
            }
        }

        public static void UpdateSurfaceArea(this IPointerDevice pointer, Vector2 size)
        {
            if (pointer != null)
            {
                var methodInfo = typeof(PointerDeviceBase).GetMethod("SetSurfaceSize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                methodInfo.Invoke(pointer, new object[] { size });
            }
        }
    }
}
