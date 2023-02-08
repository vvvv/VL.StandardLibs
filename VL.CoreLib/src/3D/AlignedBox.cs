using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VL.Core;
using Stride.Core.Mathematics;

namespace VL.Lib.Mathematics
{
    public static class AlignedBoxExtensions
    {
        /// <summary>
        /// Creates an AlignedBox from center position and size vector
        /// </summary>
        /// <param name="center"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static BoundingBox CreateCenterSize(ref Vector3 center, ref Vector3 size)
        {
            var extends = size * 0.5f;
            var result = new BoundingBox(center - extends, center + extends);
            return result;
        }
    }
}
