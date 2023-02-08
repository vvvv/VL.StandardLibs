using System;
using System.Runtime.CompilerServices;
using VL.Core;

namespace VL.Lib.Control
{
    public static class SwitchNodes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Switch<T>(bool condition, T @false, T @true) => condition ? @true : @false;

        public static void SwitchOutput<T>(bool condition, T input, T @default, out T @true, out T @false)
        {
            if (condition)
            {
                @true = input;
                @false = @default;
            }
            else
            {
                @true = @default;
                @false = input;
            }
        }

        public static void Swap<T>(bool condition, T input, T input2, out T output, out T output2)
        {
            if (condition)
            {
                output = input2;
                output2 = input;
            }
            else
            {
                output = input;
                output2 = input2;
            }
        }
    }
}
