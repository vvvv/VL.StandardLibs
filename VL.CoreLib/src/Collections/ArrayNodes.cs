using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace VL.Lib.Collections
{
    public static class ArrayNodes
    {
        public static T[] Create<T>(int length)
        {
            return new T[length];
        }

        public static T[] Reverse<T>(T[] input)
        {
            Array.Reverse(input);
            return input;
        }

        public static T[] ReverseRange<T>(T[] input, int index, int count)
        {
            Array.Reverse(input, index, count);
            return input;
        }
    }
}
