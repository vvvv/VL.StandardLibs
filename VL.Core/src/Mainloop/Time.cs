using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace VL.Lib.Animation
{
    public struct Time
    {
        public readonly double Seconds;

        public Time(double seconds)
        {
            Seconds = seconds;
        }

        /// <summary>
        /// Elapsed time in seconds since midnight year 0 of the gregorian calendar
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Time FromDateTime(DateTime input)
        {
            return (new TimeSpan(input.Ticks)).TotalSeconds;
        }

        /// <summary>
        /// Elapsed time in seconds since midnight year 0 of the gregorian calendar
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Time FromDateTimeUTC(DateTimeOffset input)
        {
            return (new TimeSpan(input.UtcTicks)).TotalSeconds;
        }

        public static TimeSpan ToTimeSpan(Time input)
        {
            return TimeSpan.FromSeconds(input.Seconds);
        }

        public static bool operator ==(Time input, Time input2)
        {
            return input.Seconds == input2.Seconds;
        }

        public static bool operator !=(Time input, Time input2)
        {
            return input.Seconds != input2.Seconds;
        }

        public static bool operator <(Time input, Time input2)
        {
            return input.Seconds < input2.Seconds;
        }

        public static bool operator <=(Time input, Time input2)
        {
            return input.Seconds <= input2.Seconds;
        }

        public static bool operator >(Time input, Time input2)
        {
            return input.Seconds > input2.Seconds;
        }

        public static bool operator >=(Time input, Time input2)
        {
            return input.Seconds >= input2.Seconds;
        }

        public static Time operator +(Time input, Time input2)
        {
            return new Time(input.Seconds + input2.Seconds);
        }

        public static Time operator -(Time input, Time input2)
        {
            return new Time(input.Seconds - input2.Seconds);
        }

        public static implicit operator Time(TimeSpan input)
        {
            return input.TotalSeconds;
        }

        public static implicit operator Time(double input)
        {
            return new Time(input);
        }

        public string ToString(string format = "0.0000")
        {
            return Seconds.ToString(format, CultureInfo.InvariantCulture);
        }

        public override bool Equals(object obj)
        {
            return obj is Time && Seconds == ((Time)obj).Seconds;
        }

        public override int GetHashCode()
        {
            return Seconds.GetHashCode();
        }

        public override string ToString()
        {
            return ToString();
        }
    }
}
