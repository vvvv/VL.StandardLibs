using System;
using System.Collections.Generic;
using System.Text;

namespace VL.Core
{
    /// <summary>
    /// Creates TimeSpans in a precise way, normal "From" methods do weird rounding.
    /// </summary>
    public static class TimeSpanUtils
    {
        /// <summary>
        /// Creates a TimeSpan with precise tick count, normal "From" methods do weird rounding.
        /// </summary>
        public static TimeSpan FromMillisecondsPrecise(double milliSeconds)
            => TimeSpan.FromTicks((long)(milliSeconds * TimeSpan.TicksPerMillisecond));

        /// <summary>
        /// Creates a TimeSpan with precise tick count, normal "From" methods do weird rounding.
        /// </summary>
        public static TimeSpan FromSecondsPrecise(double seconds)
            => TimeSpan.FromTicks((long)(seconds * TimeSpan.TicksPerSecond));

        /// <summary>
        /// Creates a TimeSpan with precise tick count, normal "From" methods do weird rounding.
        /// </summary>
        public static TimeSpan FromMinutesPrecise(double minutes)
            => TimeSpan.FromTicks((long)(minutes * TimeSpan.TicksPerMinute));
    }
}
