using Stride.Core.Mathematics;
using VL.Lib.IO.Notifications;

namespace VL.Skia
{
    public class LinkedLayerBase : ILayer
    {
        protected ILayer Input;

        public virtual void Render(CallerInfo caller)
        {
            Input?.Render(caller);
        }

        public virtual bool Notify(INotification notification, CallerInfo caller)
        {
            return Input?.Notify(notification, caller) ?? false;
        }

        public virtual RectangleF? Bounds { get => Input?.Bounds; }
    }
}
