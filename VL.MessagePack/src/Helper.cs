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

        private static ReadOnlyMemory<byte> ToBytes<T>(this T input) where T : unmanaged
        {
            return ReadOnlyMemoryExtensions.AsBytes(new ReadOnlyMemory<T>(new T[] { input }));
        }
        public static ReadOnlyMemory<byte> ToBytes<T>(this Spread<T> input) where T : unmanaged
        {
            return ReadOnlyMemoryExtensions.Cast<T, byte>(input.AsMemory());
        }
        public static Spread<T> FromBytes<T>(this ReadOnlyMemory<byte> input) where T : unmanaged
        {
            var span = MemoryMarshal.Cast<byte, T>(input.Span);
            var builder = Pooled.GetArrayBuilder<T>();
            builder.Value.Count = span.Length;
            span.CopyTo(builder.Value.AsSpan());
            return Spread.Create<T>(builder.ToImmutableAndFree());
        }
        public static ReadOnlyMemory<byte> ToBytes<T>(this string input) where T : unmanaged
        {
            return new ReadOnlyMemory<byte>(UTF8Encoding.UTF8.GetBytes(input));
        }


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
