
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace VL.Lib.Primitive.TypeHelpers
{
    public static class TypeHelpers
    {
        /// <summary>
        /// Checks whether the input is an integer type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsInteger<T>(T input)
        {
            // see c# integral types
            // https://msdn.microsoft.com/en-us/library/exx3b86w.aspx
            var type = typeof(T);
            return type == typeof(int)
                || type == typeof(uint)
                || type == typeof(long)
                || type == typeof(ulong)
                || type == typeof(byte)
                || type == typeof(sbyte)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(char);
        }

        public static void ConstrainTypes<T>(T input, T input2, T input3, T input4)
        {
        }       
    }
}
