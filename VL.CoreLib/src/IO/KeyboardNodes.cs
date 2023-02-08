using System;

namespace VL.Lib.IO
{
    public static class KeyboardNodes
    {
        public static Keys FromKeyName(string input)
        {
            Keys result;
            if (Enum.TryParse(input, out result))
                return result;
            return Keys.None;
        }

        public static string ToKeyName(this Keys keyCode)
        {
            return keyCode.ToString();
        }
    }
}
