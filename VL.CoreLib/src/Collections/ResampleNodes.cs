using System;
using System.Collections.Generic;
using System.Linq;
using VL.Core;

namespace VL.Lib.Collections
{
    public static class ResampleNodes
    {
        public static float HermiteInterpolate(float y0, float y1, float y2, float y3, float phase, float tension, float bias)
        {
            var mu2 = phase * phase;
            var mu3 = mu2 * phase;

            var m0 = (y1 - y0) * (1 + bias) * (1 - tension) / 2;
            m0 += (y2 - y1) * (1 - bias) * (1 - tension) / 2;

            var m1 = (y2 - y1) * (1 + bias) * (1 - tension) / 2;
            m1 += (y3 - y2) * (1 - bias) * (1 - tension) / 2;

            var a0 = 2 * mu3 - 3 * mu2 + 1;
            var a1 = mu3 - 2 * mu2 + phase;
            var a2 = mu3 - mu2;
            var a3 = -2 * mu3 + 3 * mu2;

            return (a0 * y1 + a1 * m0 + a2 * m1 + a3 * y2);
        }
    }
}

