
using System;
using System.Collections.Generic;
using System.Threading;
using Stride.Core.Collections;

namespace Stride.Input
{
    /// <summary>
    /// Pools input events of a given type
    /// </summary>
    public static class PointerPointPool
    {
        private static ThreadLocal<PointPool> pool;

        static PointerPointPool()
        {
            pool = new ThreadLocal<PointPool>(
                () => new PointPool());
        }

        /// <summary>
        /// The number of events in circulation, if this number keeps increasing, Enqueue is possible not called somewhere
        /// </summary>
#pragma warning disable CS8602 
        public static int ActiveObjects => pool.Value.ActiveObjects;


        private static PointerPoint CreateEvent()
        {
            return new PointerPoint();
        }

        /// <summary>
        /// Retrieves a new event that can be used, either from the pool or a new instance
        /// </summary>
        /// <param name="device">The device that generates this event</param>
        /// <returns>An event</returns>
        public static PointerPoint GetOrCreate(IPointerDevice device)
        {
            return pool.Value.GetOrCreate(device);
        }

        /// <summary>
        /// Puts a used event back into the pool to be recycled
        /// </summary>
        /// <param name="item">The event to reuse</param>
        public static void Enqueue(PointerPoint item)
        {
            pool.Value.Enqueue(item);
        }
#pragma warning restore CS8602 
        /// <summary>
        /// Pool class, since <see cref="PoolListStruct{T}"/> can not be placed inside <see cref="ThreadLocal{T}"/>
        /// </summary>
        private class PointPool
        {
            private PoolListStruct<PointerPoint> pool = new PoolListStruct<PointerPoint>(8, CreateEvent);

            public int ActiveObjects => pool.Count;

            public PointerPoint GetOrCreate(IPointerDevice device)
            {
                PointerPoint item = pool.Add();
                item.Pointer = device;
                return item;
            }

            public void Enqueue(PointerPoint item)
            {
                item.Pointer = null;
                pool.Remove(item);
            }
        }
    }
}
