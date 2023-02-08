using System;
using System.Collections.Generic;

namespace VL.Lib.Mathematics
{
    public enum RangeMapping
    {
        /// <summary>
        /// No mapping.
        /// </summary>
        None,

        /// <summary>
        /// Values are clamped at the borders.
        /// </summary>
        Clamp,

        /// <summary>
        /// Values are mirrored at the borders.
        /// </summary>
        Mirror,

        /// <summary>
        /// Values are wrapped around the borders.
        /// </summary>
        Wrap
    };

    // Useful operations like Width, Center are defined in VL using adaptive nodes (not available in C#)
    public struct Range<T> : IEquatable<Range<T>>, IFormattable
    {
        static readonly EqualityComparer<T> Comparer = EqualityComparer<T>.Default;

        public readonly T From;
        public readonly T To;

        public Range(T from, T to)
        {
            From = from;
            To = to;
        }

        public void Split(out T from, out T to)
        {
            from = From;
            to = To;
        }

        public bool Equals(Range<T> other) => Comparer.Equals(From, other.From) && Comparer.Equals(To, other.To);

        public override bool Equals(object obj) => (obj is Range<T> range) ? Equals(range) : false;

        public override int GetHashCode() => From.GetHashCode() ^ To.GetHashCode();

        public override string ToString() => $"{From} .. {To}";

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (From is IFormattable f && To is IFormattable t)
                return $"{f.ToString(format, formatProvider)} .. {t.ToString(format, formatProvider)}";
            return ToString();
        }

        public static bool operator ==(Range<T> a, Range<T> b) => a.Equals(b);

        public static bool operator !=(Range<T> a, Range<T> b) => !a.Equals(b);
    }
}
