using System;
using System.Globalization;
using Stride.Core.Mathematics;

namespace VL.Lib.Primitive
{
    internal static class VectorParser
    {
        // Fast character classification
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static bool IsWhiteSpace(char c) => c == ' ' || c == '\t' || c == '\r' || c == '\n';

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static bool IsDelimiter(char c) => c == ',' || c == ';' || c == ' ' || c == '\t' || c == '|';

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static bool IsBracket(char c) => c == '(' || c == ')' || c == '[' || c == ']' || c == '{' || c == '}' || c == '<' || c == '>';

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static bool IsDigitOrSign(char c) => (c >= '0' && c <= '9') || c == '+' || c == '-' || c == '.' || c == 'e' || c == 'E';

        // Extract numeric values from various formats
        // Returns true if the expected component count matches
        private static bool ExtractComponents(ReadOnlySpan<char> value, Span<Range> componentRanges, int expectedCount, out int actualCount)
        {
            actualCount = 0;
            if (value.IsEmpty || componentRanges.Length < expectedCount)
                return false;

            int length = value.Length;
            int pos = 0;

            // Skip leading whitespace and brackets
            while (pos < length && (IsWhiteSpace(value[pos]) || IsBracket(value[pos])))
                pos++;

            // Check for labeled format: "X:1.0 Y:2.0" or "X=1.0, Y=2.0"
            bool isLabeledFormat = false;
            if (pos + 1 < length &&
                (value[pos] == 'X' || value[pos] == 'x') &&
                (value[pos + 1] == ':' || value[pos + 1] == '='))
            {
                isLabeledFormat = true;
            }

            if (isLabeledFormat)
            {
                // Parse labeled format
                char[] expectedLabels = { 'X', 'Y', 'Z', 'W' };

                for (int i = 0; i < expectedCount; i++)
                {
                    // Skip whitespace
                    while (pos < length && IsWhiteSpace(value[pos]))
                        pos++;

                    // Check for label (case insensitive)
                    if (pos >= length ||
                        (char.ToUpperInvariant(value[pos]) != expectedLabels[i]))
                        return false;

                    pos++; // Skip label letter

                    // Skip whitespace
                    while (pos < length && IsWhiteSpace(value[pos]))
                        pos++;

                    // Expect ':' or '='
                    if (pos >= length || (value[pos] != ':' && value[pos] != '='))
                        return false;

                    pos++; // Skip separator

                    // Skip whitespace
                    while (pos < length && IsWhiteSpace(value[pos]))
                        pos++;

                    // Find the start of the number
                    int numStart = pos;

                    // Find the end of the number (stop at delimiter, whitespace for next label, or bracket)
                    while (pos < length && IsDigitOrSign(value[pos]))
                        pos++;

                    if (pos == numStart)
                        return false;

                    componentRanges[actualCount++] = new Range(numStart, pos);

                    // Skip trailing delimiter/whitespace
                    while (pos < length && (IsDelimiter(value[pos]) || IsWhiteSpace(value[pos])))
                        pos++;
                }
            }
            else
            {
                // Parse simple delimited format
                for (int i = 0; i < expectedCount && pos < length; i++)
                {
                    // Skip whitespace and delimiters
                    while (pos < length && (IsWhiteSpace(value[pos]) || IsDelimiter(value[pos]) || IsBracket(value[pos])))
                        pos++;

                    if (pos >= length)
                        return false;

                    int numStart = pos;

                    // Find the end of the number
                    while (pos < length && IsDigitOrSign(value[pos]))
                        pos++;

                    if (pos == numStart)
                        return false;

                    componentRanges[actualCount++] = new Range(numStart, pos);
                }
            }

            return actualCount == expectedCount;
        }

        // Public parsing methods matching TryParseDelegate signature
        public static bool TryParseVector2(string value, NumberStyles style, IFormatProvider format, out Vector2 result)
        {
            result = Vector2.Zero;

            Span<Range> ranges = stackalloc Range[2];
            if (!ExtractComponents(value.AsSpan(), ranges, 2, out int count))
                return false;

            ReadOnlySpan<char> span = value.AsSpan();

            if (!float.TryParse(span[ranges[0]], style, format, out float x))
                return false;

            if (!float.TryParse(span[ranges[1]], style, format, out float y))
                return false;

            result = new Vector2(x, y);
            return true;
        }

        public static bool TryParseVector3(string value, NumberStyles style, IFormatProvider format, out Vector3 result)
        {
            result = Vector3.Zero;

            Span<Range> ranges = stackalloc Range[3];
            if (!ExtractComponents(value.AsSpan(), ranges, 3, out int count))
                return false;

            ReadOnlySpan<char> span = value.AsSpan();

            if (!float.TryParse(span[ranges[0]], style, format, out float x))
                return false;

            if (!float.TryParse(span[ranges[1]], style, format, out float y))
                return false;

            if (!float.TryParse(span[ranges[2]], style, format, out float z))
                return false;

            result = new Vector3(x, y, z);
            return true;
        }

        public static bool TryParseVector4(string value, NumberStyles style, IFormatProvider format, out Vector4 result)
        {
            result = Vector4.Zero;

            Span<Range> ranges = stackalloc Range[4];
            if (!ExtractComponents(value.AsSpan(), ranges, 4, out int count))
                return false;

            ReadOnlySpan<char> span = value.AsSpan();

            if (!float.TryParse(span[ranges[0]], style, format, out float x))
                return false;

            if (!float.TryParse(span[ranges[1]], style, format, out float y))
                return false;

            if (!float.TryParse(span[ranges[2]], style, format, out float z))
                return false;

            if (!float.TryParse(span[ranges[3]], style, format, out float w))
                return false;

            result = new Vector4(x, y, z, w);
            return true;
        }

        public static bool TryParseInt2(string value, NumberStyles style, IFormatProvider format, out Int2 result)
        {
            result = Int2.Zero;

            Span<Range> ranges = stackalloc Range[2];
            if (!ExtractComponents(value.AsSpan(), ranges, 2, out int count))
                return false;

            ReadOnlySpan<char> span = value.AsSpan();

            if (!int.TryParse(span[ranges[0]], style, format, out int x))
                return false;

            if (!int.TryParse(span[ranges[1]], style, format, out int y))
                return false;

            result = new Int2(x, y);
            return true;
        }

        public static bool TryParseInt3(string value, NumberStyles style, IFormatProvider format, out Int3 result)
        {
            result = Int3.Zero;

            Span<Range> ranges = stackalloc Range[3];
            if (!ExtractComponents(value.AsSpan(), ranges, 3, out int count))
                return false;

            ReadOnlySpan<char> span = value.AsSpan();

            if (!int.TryParse(span[ranges[0]], style, format, out int x))
                return false;

            if (!int.TryParse(span[ranges[1]], style, format, out int y))
                return false;

            if (!int.TryParse(span[ranges[2]], style, format, out int z))
                return false;

            result = new Int3(x, y, z);
            return true;
        }

        public static bool TryParseInt4(string value, NumberStyles style, IFormatProvider format, out Int4 result)
        {
            result = Int4.Zero;

            Span<Range> ranges = stackalloc Range[4];
            if (!ExtractComponents(value.AsSpan(), ranges, 4, out int count))
                return false;

            ReadOnlySpan<char> span = value.AsSpan();

            if (!int.TryParse(span[ranges[0]], style, format, out int x))
                return false;

            if (!int.TryParse(span[ranges[1]], style, format, out int y))
                return false;

            if (!int.TryParse(span[ranges[2]], style, format, out int z))
                return false;

            if (!int.TryParse(span[ranges[3]], style, format, out int w))
                return false;

            result = new Int4(x, y, z, w);
            return true;
        }
    }
}