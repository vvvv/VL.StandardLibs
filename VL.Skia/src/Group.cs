using Stride.Core.Mathematics;
using System.Collections.Generic;
using System.Linq;
using VL.Lib.IO.Notifications;
using VL.Lib.Mathematics;

namespace VL.Skia
{
    static class StackHelpers
    {
        public const int maxStackCount = 10;
    }

    public class Group : LinkedLayerBase
    {
        ILayer Input2;
        int stackCount;

        public void Update(ILayer input, ILayer input2, out ILayer output)
        {
            Input = input;
            Input2 = input2;
            output = this;
        }

        public override void Render(CallerInfo caller)
        {
            if (stackCount >= StackHelpers.maxStackCount)
                return;

            stackCount++;
            try
            {
                base.Render(caller);
                Input2?.Render(caller);
            }
            finally
            {
                stackCount--;
            }
        }

        public override bool Notify(INotification notification, CallerInfo caller)
        {
            if (stackCount >= StackHelpers.maxStackCount)
                return true;

            stackCount++;
            try
            {
                if (Input2 != null && Input2.Notify(notification, caller))
                    return true;
                return base.Notify(notification, caller);
            }
            finally
            {
                stackCount--;
            }
        }
        public override RectangleF? Bounds => Input.ConcatIfNotNull(Input2).Select(i => i?.Bounds).Union();
    }

    public class SpectralGroup : ILayer
    {
        IEnumerable<ILayer> Inputs;
        int stackCount;

        public void Update(IEnumerable<ILayer> input, out ILayer output)
        {
            Inputs = input;
            output = this;
        }

        public void Render(CallerInfo caller)
        {
            if (stackCount >= StackHelpers.maxStackCount)
                return;

            stackCount++;
            try
            {
                if (Inputs.TryGetSpan(out var span))
                {
                    // Allocation free iteration
                    foreach (var i in span)
                        i?.Render(caller);
                }
                else
                {
                    foreach (var i in Inputs)
                        i?.Render(caller);
                }
            }
            finally
            {
                stackCount--;
            }
        }

        public bool Notify(INotification notification, CallerInfo caller)
        {
            if (stackCount >= StackHelpers.maxStackCount)
                return true;

            stackCount++;
            try
            {
                foreach (var i in Inputs.Reverse())
                    if (i != null && i.Notify(notification, caller))
                        return true;
                return false;
            }
            finally
            {
                stackCount--;
            }
        }

        public RectangleF? Bounds => Inputs.Select(i => i?.Bounds).Union();
    }
}

