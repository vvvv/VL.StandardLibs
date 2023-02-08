using System;
using System.Collections.Generic;
using VL.Lib.Reactive;

namespace VL.Lib.Collections
{
    /// <summary>
    /// Holds on to a copy of the latest received collection. Whenever a collection gets pushed to the node a copy is made and stored internally.
    /// </summary>
    public class HoldLatestCopy<T> : HoldLatestCopy<IReadOnlyList<T>, ArrayBuilder<T>>
    {
        public HoldLatestCopy(IReadOnlyList<T> initialResult) : base(initialResult)
        {
        }

        // HACK: Override needed as forwarding mechanism in VL can't properly deal with generic base class
        public override IReadOnlyList<T> Update(IObservable<IReadOnlyList<T>> asyncNotifications, int timeout, bool reset, out int swapCount, out int dropCount)
        {
            return base.Update(asyncNotifications, timeout, reset, out swapCount, out dropCount);
        }

        protected override void CopyTo(IReadOnlyList<T> source, ref ArrayBuilder<T> destination)
        {
            if (destination is null)
            {
                destination = new ArrayBuilder<T>(source.Count);
                destination.AddRange(source);
            }
            else
            {
                destination.Clear();
                destination.AddRange(source);
            }
        }
    }
}
