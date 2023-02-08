using System;
using System.Globalization;

namespace VL.Core
{
    public delegate bool TryParseDelegate<T>(string value, NumberStyles numberStyle, IFormatProvider format, out T result);

    public static class ParseUtils
    {
        /// <summary>
        /// The VL allowed number style, its any but without thousands separator
        /// </summary>
        public const NumberStyles VLUserInputNumberStyle = NumberStyles.Any & ~NumberStyles.AllowThousands;
        static readonly CultureInfo CommaCulture = CultureInfo.GetCultureInfo("de-DE");

        public static bool TryParseValue<T>(string value, TryParseDelegate<T> tryMethod, out T result)
        {
            //try parsing a hex string
            var x = value.TrimStart('0').ToLower();
            if (x.StartsWith("x") || x.StartsWith("#"))
            {
                x = x.TrimStart('x', '#');
                if (tryMethod(x, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out result))
                    return true;
            }
            //Try parsing in the current culture
            else if (tryMethod(value, VLUserInputNumberStyle, CultureInfo.CurrentCulture, out result) ||
                //or in neutral culture
                tryMethod(value, VLUserInputNumberStyle, CultureInfo.InvariantCulture, out result) ||
                //or as fallback a culture that has ',' as comma separator
                tryMethod(value, VLUserInputNumberStyle, CommaCulture, out result))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to parse the string as float with the VL allowed Number style
        /// </summary>
        /// <param name="value"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryParseFloat(string value, out float result)
        {
            return TryParseValue(value, float.TryParse, out result);
        }

        /// <summary>
        /// Tries to parse the string as integer with the VL allowed number style
        /// </summary>
        /// <param name="value"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryParseInt(string value, out int result)
        {
            return TryParseValue(value, int.TryParse, out result);
        }

        /// <summary>
        /// Tries to parse the string as unsigned integer with the VL allowed number style
        /// </summary>
        /// <param name="value"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryParseUInt(string value, out uint result)
        {
            return TryParseValue(value, uint.TryParse, out result);
        }
    }
}
