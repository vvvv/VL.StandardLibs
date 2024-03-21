using Stride.Core.Collections;
using Stride.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Input
{
    using Stride.Core.Collections;

    public class PointerBase
    {
        private readonly HashSet<PointerPoint> pressedPointers = new HashSet<PointerPoint>();
        private readonly HashSet<PointerPoint> releasedPointers = new HashSet<PointerPoint>();
        private readonly HashSet<PointerPoint> downPointers = new HashSet<PointerPoint>();

        private IReadOnlySet<PointerPoint> _pressedPointers;
        private IReadOnlySet<PointerPoint> _releasedPointers;
        private IReadOnlySet<PointerPoint> _downPointers;

        public PointerBase()
        {
            _pressedPointers = new ReadOnlySet<PointerPoint>(pressedPointers);
            _releasedPointers = new ReadOnlySet<PointerPoint>(releasedPointers);
            _downPointers = new ReadOnlySet<PointerPoint>(downPointers);
        }

        private void transformAndFillSet(IReadOnlySet<PointerPoint> pointers, HashSet<PointerPoint> pointerset, IPointerDevice pointer, MappedInputSource source, IPointerDevice device)
        {
            pointerset.Clear();

            foreach (var point in pointers)
            {
                var transformedPoint = PointerPointPool.GetOrCreate(device);
                if (transformedPoint != null)
                {
                    transformedPoint.Position = point.Position.transformPos(pointer, source);
                    transformedPoint.Delta = point.Delta.transformDelta(pointer, source);
                    transformedPoint.IsDown = point.IsDown;
                    transformedPoint.Id = point.Id;
                    pointerset.Add(transformedPoint);
                }
            }
        }

        public IReadOnlySet<PointerPoint> GetPressedPointers(IPointerDevice pointer, MappedInputSource source, IPointerDevice device)
        {
            transformAndFillSet(pointer.PressedPointers, pressedPointers, pointer, source, device);
            return _pressedPointers;
        }
        public IReadOnlySet<PointerPoint> GetReleasedPointers(IPointerDevice pointer, MappedInputSource source, IPointerDevice device)
        {
            transformAndFillSet(pointer.ReleasedPointers, releasedPointers, pointer, source, device);
            return _releasedPointers;
        }
        public IReadOnlySet<PointerPoint> GetDownPointers(IPointerDevice pointer, MappedInputSource source, IPointerDevice device)
        {
            transformAndFillSet(pointer.DownPointers, downPointers, pointer, source, device);
            return _downPointers;
        }
    }
}
