using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Animation;
using VL.Lib.Collections;
using VL.Lib.Primitive;
using VL.Lib.Text;

[assembly: ImportType(typeof(StringNodes))]

namespace VL.Lib.Primitive
{
    delegate bool TryParseDelegate<T>(string value, NumberStyles numberStyle, IFormatProvider format, out T result);

    public static class StringNodes
    {
        [Category("Primitive.String")]
        public static string Format(string format, [Pin(PinGroupKind = Model.PinGroupKind.Collection, PinGroupDefaultCount = 1)] object[] input)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args: input);
        }
    }

    public static class StringExtensions
    {
        public static string Plus(string input, string input2)
        {
            return input + input2;
        }

        public static readonly string NewLine = Environment.NewLine;
        public static readonly string CR = "\r";
        public static readonly string LF = "\n";
        public static readonly string CRLF = "\r\n";
        public static readonly string Tab = "\t";

        /// <summary>
        /// Adds the given quote string at the beginning and end of the string
        /// </summary>
        /// <param name="text"></param>
        /// <param name="quotes"></param>
        /// <returns></returns>
        public static string SurroundWith(this string text, string quotes)
        {
            return quotes + text + quotes;
        }

        /// <summary>
        /// Adds the given quote char at the beginning and end of the string
        /// </summary>
        /// <param name="text"></param>
        /// <param name="quotes"></param>
        /// <returns></returns>
        public static string SurroundWith(this string text, char quotes)
        {
            return quotes + text + quotes;
        }

        public static string FormatSequence<T>(string format, IEnumerable<T> inputs)
        {
            return string.Format(CultureInfo.InvariantCulture, format, inputs.Cast<object>().ToArray());
        }

        public static unsafe string FromBytes(IEnumerable<byte> input, Encodings encoding)
        {
            var e = encoding.ToEncoding();
            if (input.TryGetSpan(out var span))
                return GetString(span, e);
            else
                return GetString(input.ToArray(), e);
        }

        private static unsafe string GetString(ReadOnlySpan<byte> bytes, Encoding encoding)
        {
            if (bytes.IsEmpty)
                return string.Empty;

            // For UTF8 we disabled the BOM (GetPreamble returns nothing). Therefor switch to the one with the preamble.
            if (encoding is UTF8Encoding)
            {
                encoding = Encoding.UTF8;
            }

            // Get rid of the BOM. The encoding is not doing that on its own. See remarks of https://docs.microsoft.com/en-us/dotnet/api/system.text.utf8encoding.getstring?view=net-5.0
            var preamble = encoding.GetPreamble();
            if (bytes.StartsWith(preamble))
            {
                bytes = bytes.Slice(preamble.Length);
            }

            fixed (byte* ptr = bytes)
            {
                return encoding.GetString(ptr, bytes.Length);
            }
        }

        public static byte[] ToBytes(string input, Encodings encoding)
        {
            return encoding.ToEncoding().GetBytes(input);
        }

        //value parsing

        const NumberStyles NoThousands = NumberStyles.Any & ~NumberStyles.AllowThousands;

        static bool TryParseValue<T>(string value, TryParseDelegate<T> tryMethod, out T result)
        {
            //Try parsing in the neutral culture
            if (!tryMethod(value, NoThousands, CultureInfo.InvariantCulture, out result) &&
                //or in local language
                !tryMethod(value, NoThousands, CultureInfo.CurrentCulture, out result))
            {
                return false;
            }

            return true;
        }

        public static bool TryParse(string @string, out float value)
        {
            return TryParseValue(@string, float.TryParse, out value);
        }

        public static bool TryParse(string @string, out double value)
        {
            return TryParseValue(@string, double.TryParse, out value);
        }

        public static bool TryParse(string @string, out short value)
        {
            return TryParseValue(@string, short.TryParse, out value);
        }

        public static bool TryParse(string @string, out ushort value)
        {
            return TryParseValue(@string, ushort.TryParse, out value);
        }

        public static bool TryParse(string @string, out int value)
        {
            return TryParseValue(@string, int.TryParse, out value);
        }

        public static bool TryParse(string @string, out uint value)
        {
            return TryParseValue(@string, uint.TryParse, out value);
        }

        public static bool TryParse(string @string, out long value)
        {
            return TryParseValue(@string, long.TryParse, out value);
        }

        public static bool TryParse(string @string, out ulong value)
        {
            return TryParseValue(@string, ulong.TryParse, out value);
        }

        public static bool TryParse(string @string, out sbyte value)
        {
            return TryParseValue(@string, sbyte.TryParse, out value);
        }

        public static bool TryParse(string @string, out byte value)
        {
            return TryParseValue(@string, byte.TryParse, out value);
        }

        public static bool TryParse(string @string, out bool value)
        {
            return bool.TryParse(@string, out value);
        }

        public static bool TryParse(string @string, out DateTime value)
        {
            return DateTime.TryParse(@string, out value);
        }

        public static bool TryParse(string @string, out DateTimeOffset value)
        {
            return DateTimeOffset.TryParse(@string, out value);
        }

        public static bool TryParse(string @string, out TimeSpan value)
        {
            return TimeSpan.TryParse(@string, out value);
        }

        public static bool TryParse(string @string, out Time value)
        {
            double val;
            var success = TryParseValue<double>(@string, double.TryParse, out val);
            value = val;
            return success;
        }
    }
}
