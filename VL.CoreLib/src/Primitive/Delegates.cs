using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace VL.Lib.Primitive
{
    public static class DelegateHelpers
    {
        public static Tuple<U, V> Apply1in2out<T, U, V>(T input, Func<T, Tuple<U, V>> f)
        {
            return f(input);
        }

        public static void IfFalse(bool condition, Action ifFalse)
        {
            if (!condition)
                ifFalse();
        }

        public static void IfTrue(bool condition, Action ifTrue)
        {
            if (condition)
                ifTrue();
        }

        public static void If(bool condition, Action ifTrue, Action ifFalse)
        {
            if (condition)
                ifTrue();
            else
                ifFalse();
        }

        public static T IfFalse<T>(bool condition, T ifTrue, Func<T> ifFalse)
        {
            if (!condition)
                return ifFalse();
            else
                return ifTrue;
        }

        public static T IfTrue<T>(bool condition, T ifFalse, Func<T> ifTrue)
        {
            if (condition)
                return ifTrue();
            else
                return ifFalse;
        }

        public static T If<T>(bool condition, Func<T> ifTrue, Func<T> ifFalse)
        {
            if (condition)
                return ifTrue();
            else
                return ifFalse();
        }
    }
}
