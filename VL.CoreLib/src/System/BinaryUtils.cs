using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using VL.Lib.Collections;

namespace VL.Lib.Primitive
{
    public static class BinaryUtils
    {
        public static bool ToBoolean(Spread<byte> input, int index)
        {
            return input.GetSlice(default, index) != 0;
        }

        public static sbyte ToInt8(Spread<byte> input, int index)
        {
            return IntegerConversions.ToInt8(input.GetSlice(default, index));
        }

        public static char ToChar(Spread<byte> input, int startIndex, bool inputIsBigEndian)
        {
            var s = input.AsSpan().Slice(startIndex);
            var x = inputIsBigEndian ? BinaryPrimitives.ReadUInt16BigEndian(s) : BinaryPrimitives.ReadUInt16LittleEndian(s);
            return Unsafe.BitCast<ushort, char>(x);
        }

        public static float ToFloat32(Spread<byte> input, int startIndex, bool inputIsBigEndian)
        {
            var s = input.AsSpan().Slice(startIndex);
            var x = inputIsBigEndian ? BinaryPrimitives.ReadUInt32BigEndian(s) : BinaryPrimitives.ReadUInt32LittleEndian(s);
            return Unsafe.BitCast<uint, float>(x);
        }

        public static double ToFloat64(Spread<byte> input, int startIndex, bool inputIsBigEndian)
        {
            var s = input.AsSpan().Slice(startIndex);
            var x = inputIsBigEndian ? BinaryPrimitives.ReadUInt64BigEndian(s) : BinaryPrimitives.ReadUInt64LittleEndian(s);
            return Unsafe.BitCast<ulong, double>(x);
        }

        public static short ToInt16(Spread<byte> input, int startIndex, bool inputIsBigEndian)
        {
            var s = input.AsSpan().Slice(startIndex);
            return inputIsBigEndian ? BinaryPrimitives.ReadInt16BigEndian(s) : BinaryPrimitives.ReadInt16LittleEndian(s);
        }

        public static int ToInt32(Spread<byte> input, int startIndex, bool inputIsBigEndian)
        {
            var s = input.AsSpan().Slice(startIndex);
            return inputIsBigEndian ? BinaryPrimitives.ReadInt32BigEndian(s) : BinaryPrimitives.ReadInt32LittleEndian(s);
        }

        public static long ToInt64(Spread<byte> input, int startIndex, bool inputIsBigEndian)
        {
            var s = input.AsSpan().Slice(startIndex);
            return inputIsBigEndian ? BinaryPrimitives.ReadInt64BigEndian(s) : BinaryPrimitives.ReadInt64LittleEndian(s);
        }

        public static ushort ToUInt16(Spread<byte> input, int startIndex, bool inputIsBigEndian)
        {
            var s = input.AsSpan().Slice(startIndex);
            return inputIsBigEndian ? BinaryPrimitives.ReadUInt16BigEndian(s) : BinaryPrimitives.ReadUInt16LittleEndian(s);
        }

        public static uint ToUInt32(Spread<byte> input, int startIndex, bool inputIsBigEndian)
        {
            var s = input.AsSpan().Slice(startIndex);
            return inputIsBigEndian ? BinaryPrimitives.ReadUInt32BigEndian(s) : BinaryPrimitives.ReadUInt32LittleEndian(s);
        }

        public static ulong ToUInt64(Spread<byte> input, int startIndex, bool inputIsBigEndian)
        {
            var s = input.AsSpan().Slice(startIndex);
            return inputIsBigEndian ? BinaryPrimitives.ReadUInt64BigEndian(s) : BinaryPrimitives.ReadUInt64LittleEndian(s);
        }
    }
}
