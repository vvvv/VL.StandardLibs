using System;
using VL.Lib.Reactive;

namespace VL.Lib.Basics.Imaging
{
    /// <summary>
    /// Holds on to a copy of the latest received image. Whenever an image gets pushed to the node a copy is made and stored internally.
    /// </summary>
    public class HoldLatestCopy : HoldLatestCopy<IImage, IImage>
    {
        public HoldLatestCopy(IImage initialResult) : base(initialResult)
        {
        }

        // HACK: Override needed as forwarding mechanism in VL can't properly deal with generic base class
        public override IImage Update(IObservable<IImage> asyncNotifications, int timeout, bool reset, out int swapCount, out int dropCount)
        {
            return base.Update(asyncNotifications, timeout, reset, out swapCount, out dropCount);
        }

        protected override void CopyTo(IImage source, ref IImage destination)
        {
            source.CopyTo(ref destination);
        }
    }
}
