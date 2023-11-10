using CommunityToolkit.HighPerformance;
using MathNet.Numerics.RootFinding;
using MessagePack.Formatters;
using Newtonsoft.Json.Linq;
using Stride.Core.Mathematics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Utils;
using VL.Lib.Collections;
using VL.Lib.IO;
using VL.MessagePack.Formatters;

namespace VL.MessagePack
{

    public static class Helper
    {
        public static bool IsBlitable<T>(this IHasMemory<T> value)
        {
            return !RuntimeHelpers.IsReferenceOrContainsReferences<T>(); ;
        }
        public static bool IsBlitable<T>(this T value)
        {
            return !RuntimeHelpers.IsReferenceOrContainsReferences<T>(); ;
        }

        public static bool IsBlitable(this Type? type)
        {

            MethodInfo? MI = typeof(RuntimeHelpers).GetMethod("IsReferenceOrContainsReferences");
            if (MI != null && type != null)
            {
                MI = MI.MakeGenericMethod(new[] { type });
                var isRef = MI.Invoke(null, new object[] { });

                if (isRef != null && !(bool)isRef)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
