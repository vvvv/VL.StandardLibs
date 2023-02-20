using Stride.Core.Mathematics;
using VL.Lib.IO.Notifications;

namespace VL.Skia
{
    /// <summary>
    /// Allows to adjust the units of the coordinate space 
    /// </summary>
    public enum Sizing
    {
        /// <summary>
        /// Pixel space. One unit equals 100 acutal pixels.  
        /// </summary>
        Pixels,
        /// <summary>
        /// Device Independant Pixels are like pixels, but respect the scaling factor of the display. One unit equals 100 actual DIP.
        /// </summary>
        DIP,
        /// <summary>
        /// Adjust with and height manually. 
        /// Setting either width or height to 0 results in computing width depending on height or vice versa, 
        /// while respecting the aspect ratio of the renderer or viewport.
        /// </summary>
        ManualSize,
    }

    /// <summary>
    /// Scene graph nodes may 
    /// * render themselves into the layer
    /// * react on or modify notifications from downstream 
    /// </summary>
    public interface ILayer : IRendering, IBehavior
    {
        /// <summary>
        /// Renders the layer. Called from downstream. 
        /// Scene graph elements should render themselves before calling render upstream.
        /// </summary>
        new void Render(CallerInfo caller);

        /// <summary>
        /// Scene graph nodes typically notify upstream before considering to apply own logic.
        /// Typically only apply own logic if notification didn't get processed by upstream. 
        /// Return if notification got processed.
        /// </summary>
        new bool Notify(INotification notification, CallerInfo caller);

        /// <summary>
        /// The Bounds of the rendering
        /// </summary>
        new RectangleF? Bounds { get; }
    }

    /// <summary>
    /// Allows to concentrate on the rendering of a single layer. 
    /// No need for calling anything upstream, no need to think about notifications.
    /// </summary>
    public interface IRendering
    {
        /// <summary>
        /// Renders the layer. 
        /// </summary>
        void Render(CallerInfo caller);

        /// <summary>
        /// The Bounds of the rendering
        /// </summary>
        RectangleF? Bounds { get; }
    }

    /// <summary>
    /// Allows to concentrate on the behavior of a single layer. 
    /// No need for calling anything upstream, no need to think about rendering.
    /// </summary>
    public interface IBehavior
    {
        /// <summary>
        /// If notification got processed return true.
        /// </summary>
        bool Notify(INotification notification, CallerInfo caller);
    }
}
