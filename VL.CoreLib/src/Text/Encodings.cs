using System;
using System.Text;

namespace VL.Lib.Text
{
    public enum Encodings
    {
        SystemDefault, 
        ASCII, 
        Unicode, 
        UTF32,
        [Obsolete("The UTF-7 encoding is insecure and should not be used. Consider using UTF-8 instead.")]
        UTF7, 
        UTF8 
    }

    public static class EncodingsExtensions
    {
        // Not recommended to use BOM, see comment here: https://stackoverflow.com/questions/5266069/streamwriter-and-utf-8-byte-order-marks
        static readonly Encoding UTF8EncodingWithoutBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public static readonly Encodings DefaultEncoding = Encodings.UTF8;

        public static Encoding ToEncoding(this Encodings encoding)
        {
            switch (encoding)
            {
                case Encodings.ASCII:
                    return Encoding.ASCII;
                case Encodings.Unicode:
                    return Encoding.Unicode;
                case Encodings.UTF32:
                    return Encoding.UTF32;
#pragma warning disable CS0618, SYSLIB0001 // Type or member is obsolete
                case Encodings.UTF7:
                    return Encoding.UTF7;
#pragma warning restore CS0618, SYSLIB0001 // Type or member is obsolete
                case Encodings.UTF8:
                    return UTF8EncodingWithoutBOM;
                case Encodings.SystemDefault:
                default:
                    return Encoding.Default;
            }
        }
    }
}
