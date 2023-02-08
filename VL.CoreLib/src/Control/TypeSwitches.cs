using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using VL.Core;

namespace VL.Lib.Control.Delegation
{
    public static class TypeSwitches // these should get native regions with any output count the user wants. still only one input though. for now we only have 1 output.
    {
        public static TResult TypeSwitch<T1, TResult>(object input, Func<T1, TResult> matchFunc1, TResult defaultvalue)
        {
            return input.TypeSwitch(matchFunc1, defaultFunc: _ => defaultvalue);
        }

        public static TResult TypeSwitch2<T1, T2, TResult>(object input, Func<T1, TResult> matchFunc1, Func<T2, TResult> matchFunc2, TResult defaultvalue)
        {
            return input.TypeSwitch(matchFunc1, matchFunc2, defaultFunc: _ => defaultvalue);
        }

        public static TResult TypeSwitch3<T1, T2, T3, TResult>(object input, Func<T1, TResult> matchFunc1, Func<T2, TResult> matchFunc2, Func<T3, TResult> matchFunc3, TResult defaultvalue)
        {
            return input.TypeSwitch(matchFunc1, matchFunc2, matchFunc3, defaultFunc: _ => defaultvalue);
        }
        public static TResult TypeSwitch4<T1, T2, T3, T4, TResult>(object input, Func<T1, TResult> matchFunc1, Func<T2, TResult> matchFunc2, Func<T3, TResult> matchFunc3, Func<T4, TResult> matchFunc4, TResult defaultvalue)
        {
            return input.TypeSwitch(matchFunc1, matchFunc2, matchFunc3, matchFunc4, defaultFunc: _ => defaultvalue);
        }
        public static TResult TypeSwitch5<T1, T2, T3, T4, T5, TResult>(object input, Func<T1, TResult> matchFunc1, Func<T2, TResult> matchFunc2, Func<T3, TResult> matchFunc3, Func<T4, TResult> matchFunc4, Func<T5, TResult> matchFunc5, TResult defaultvalue)
        {
            return input.TypeSwitch(matchFunc1, matchFunc2, matchFunc3, matchFunc4, matchFunc5, defaultFunc: _ => defaultvalue);
        }
        public static TResult TypeSwitch6<T1, T2, T3, T4, T5, T6, TResult>(object input, Func<T1, TResult> matchFunc1, Func<T2, TResult> matchFunc2, Func<T3, TResult> matchFunc3, Func<T4, TResult> matchFunc4, Func<T5, TResult> matchFunc5, Func<T6, TResult> matchFunc6, TResult defaultvalue)
        {
            return input.TypeSwitch(matchFunc1, matchFunc2, matchFunc3, matchFunc4, matchFunc5, matchFunc6, defaultFunc: _ => defaultvalue);
        }
        public static TResult TypeSwitch7<T1, T2, T3, T4, T5, T6, T7, TResult>(object input, Func<T1, TResult> matchFunc1, Func<T2, TResult> matchFunc2, Func<T3, TResult> matchFunc3, Func<T4, TResult> matchFunc4, Func<T5, TResult> matchFunc5, Func<T6, TResult> matchFunc6, Func<T7, TResult> matchFunc7, TResult defaultvalue)
        {
            return input.TypeSwitch(matchFunc1, matchFunc2, matchFunc3, matchFunc4, matchFunc5, matchFunc6, matchFunc7, defaultFunc: _ => defaultvalue);
        }
        public static TResult TypeSwitch8<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(object input, Func<T1, TResult> matchFunc1, Func<T2, TResult> matchFunc2, Func<T3, TResult> matchFunc3, Func<T4, TResult> matchFunc4, Func<T5, TResult> matchFunc5, Func<T6, TResult> matchFunc6, Func<T7, TResult> matchFunc7, Func<T8, TResult> matchFunc8, TResult defaultvalue)
        {
            return input.TypeSwitch(matchFunc1, matchFunc2, matchFunc3, matchFunc4, matchFunc5, matchFunc6, matchFunc7, matchFunc8, defaultFunc: _ => defaultvalue);
        }
        public static TResult TypeSwitch9<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(object input, Func<T1, TResult> matchFunc1, Func<T2, TResult> matchFunc2, Func<T3, TResult> matchFunc3, Func<T4, TResult> matchFunc4, Func<T5, TResult> matchFunc5, Func<T6, TResult> matchFunc6, Func<T7, TResult> matchFunc7, Func<T8, TResult> matchFunc8, Func<T9, TResult> matchFunc9, TResult defaultvalue)
        {
            return input.TypeSwitch(matchFunc1, matchFunc2, matchFunc3, matchFunc4, matchFunc5, matchFunc6, matchFunc7, matchFunc8, matchFunc9, defaultFunc: _ => defaultvalue);
        }
        public static TResult TypeSwitch10<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(object input, Func<T1, TResult> matchFunc1, Func<T2, TResult> matchFunc2, Func<T3, TResult> matchFunc3, Func<T4, TResult> matchFunc4, Func<T5, TResult> matchFunc5, Func<T6, TResult> matchFunc6, Func<T7, TResult> matchFunc7, Func<T8, TResult> matchFunc8, Func<T9, TResult> matchFunc9, Func<T10, TResult> matchFunc10, TResult defaultvalue)
        {
            return input.TypeSwitch(matchFunc1, matchFunc2, matchFunc3, matchFunc4, matchFunc5, matchFunc6, matchFunc7, matchFunc8, matchFunc9, matchFunc10, defaultFunc: _ => defaultvalue);
        }
        public static TResult TypeSwitch11<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(object input, Func<T1, TResult> matchFunc1, Func<T2, TResult> matchFunc2, Func<T3, TResult> matchFunc3, Func<T4, TResult> matchFunc4, Func<T5, TResult> matchFunc5, Func<T6, TResult> matchFunc6, Func<T7, TResult> matchFunc7, Func<T8, TResult> matchFunc8, Func<T9, TResult> matchFunc9, Func<T10, TResult> matchFunc10, Func<T11, TResult> matchFunc11, TResult defaultvalue)
        {
            return input.TypeSwitch(matchFunc1, matchFunc2, matchFunc3, matchFunc4, matchFunc5, matchFunc6, matchFunc7, matchFunc8, matchFunc9, matchFunc10, matchFunc11, defaultFunc: _ => defaultvalue);
        }
        public static TResult TypeSwitch12<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(object input, Func<T1, TResult> matchFunc1, Func<T2, TResult> matchFunc2, Func<T3, TResult> matchFunc3, Func<T4, TResult> matchFunc4, Func<T5, TResult> matchFunc5, Func<T6, TResult> matchFunc6, Func<T7, TResult> matchFunc7, Func<T8, TResult> matchFunc8, Func<T9, TResult> matchFunc9, Func<T10, TResult> matchFunc10, Func<T11, TResult> matchFunc11, Func<T12, TResult> matchFunc12, TResult defaultvalue)
        {
            return input.TypeSwitch(matchFunc1, matchFunc2, matchFunc3, matchFunc4, matchFunc5, matchFunc6, matchFunc7, matchFunc8, matchFunc9, matchFunc10, matchFunc11, matchFunc12, defaultFunc: _ => defaultvalue);
        }
        public static TResult TypeSwitch13<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(object input, Func<T1, TResult> matchFunc1, Func<T2, TResult> matchFunc2, Func<T3, TResult> matchFunc3, Func<T4, TResult> matchFunc4, Func<T5, TResult> matchFunc5, Func<T6, TResult> matchFunc6, Func<T7, TResult> matchFunc7, Func<T8, TResult> matchFunc8, Func<T9, TResult> matchFunc9, Func<T10, TResult> matchFunc10, Func<T11, TResult> matchFunc11, Func<T12, TResult> matchFunc12, Func<T13, TResult> matchFunc13, TResult defaultvalue)
        {
            return input.TypeSwitch(matchFunc1, matchFunc2, matchFunc3, matchFunc4, matchFunc5, matchFunc6, matchFunc7, matchFunc8, matchFunc9, matchFunc10, matchFunc11, matchFunc12, matchFunc13, defaultFunc: _ => defaultvalue);
        }
    }
}
